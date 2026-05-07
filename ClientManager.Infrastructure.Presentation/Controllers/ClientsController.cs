using Asp.Versioning;
using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Presentation.ModelBinders;
using ClientManager.Infrastructure.Presentation.Validators;
using FluentValidation;
using LoggingService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.DataTransferObjects.Clients;
using Shared.RequestFeatures;
using System.Text.Json;

namespace ClientManager.Infrastructure.Presentation.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    public class ClientsController : ControllerBase
    {
        private readonly IServiceManager _service;
        private readonly ILoggerManager _logger;

        public ClientsController(IServiceManager service, ILoggerManager logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Get a paged list of clients
        /// </summary>
        /// <param name="clientParameters">Pagination, sorting and search parameters</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="includeFounders">Whether to include founders for each client</param>
        /// <returns>The clients list with pagination metadata in the X-Pagination response header</returns>
        [HttpGet]
        [EnableRateLimiting("SpecificPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetClients(
            [FromQuery] ClientParameters clientParameters,
            CancellationToken ct,
            [FromQuery] bool includeFounders = true)
        {
            var pagedResult = await _service.ClientService.GetAllClientsAsync(clientParameters, trackChanges: false, includeFounders, ct);

            Response.Headers["X-Pagination"] = JsonSerializer.Serialize(pagedResult.metaData);

            return Ok(pagedResult.clients);
        }

        /// <summary>
        /// Get a specific client by id
        /// </summary>
        /// <param name="id">Client id</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="includeFounders">Whether to include the client's founders</param>
        /// <returns>The client</returns>
        [HttpGet("{id:guid}", Name = "ClientById")]
        [DisableRateLimiting]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetClient(
            Guid id,
            CancellationToken ct,
            [FromQuery] bool includeFounders = true)
        {
            var client = await _service.ClientService.GetClientAsync(id, trackChanges: false, includeFounders, ct);

            return Ok(client);
        }

        /// <summary>
        /// Create new client
        /// </summary>
        /// <param name="client">The client to create</param>
        /// <param name="validator">Validator (resolved from DI)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>A newly created client</returns>
        [HttpPost(Name = "CreateClient")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateClient(
            [FromBody] ClientForCreationDto client,
            [FromServices] IValidator<ClientForCreationDto> validator,
            CancellationToken ct)
        {
            if (client is null)
            {
                _logger.LogWarning("CreateClient: request body is null.");
                return BadRequest("ClientForCreationDto object is null");
            }

            var valResult = validator.Validate(client);
            if (!valResult.IsValid)
            {
                _logger.LogWarning($"CreateClient validation failed: {valResult.FormatErrors()}");
                return UnprocessableEntity(valResult.ToDictionary());
            }

            var createdClient = await _service.ClientService.CreateClientAsync(client, ct);

            return CreatedAtRoute("ClientById", new { id = createdClient.Id }, createdClient);
        }

        /// <summary>
        /// Get a collection of clients by their ids
        /// </summary>
        /// <param name="ids">Comma-separated list of client ids in the URL: /collection/(id1,id2,id3)</param>
        /// <param name="ct">Cancellation token</param>
        /// <param name="includeFounders">Whether to include founders for each client</param>
        /// <returns>The clients</returns>
        [HttpGet("collection/({ids})", Name = "ClientCollection")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetClientCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids,
            CancellationToken ct,
            [FromQuery] bool includeFounders = true)
        {
            var clients = await _service.ClientService.GetByIdsAsync(ids, trackChanges: false, includeFounders, ct);

            return Ok(clients);
        }

        /// <summary>
        /// Create a collection of clients in a single atomic operation
        /// </summary>
        /// <param name="clientCollection">The clients to create</param>
        /// <param name="validator">Validator (resolved from DI)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The newly created clients</returns>
        [HttpPost("collection")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> CreateClientCollection(
            [FromBody] IEnumerable<ClientForCreationDto> clientCollection,
            [FromServices] IValidator<ClientForCreationDto> validator,
            CancellationToken ct)
        {
            if (clientCollection is null)
            {
                _logger.LogWarning("CreateClientCollection: request body is null.");
                return BadRequest("ClientCollection object is null");
            }

            var clients = clientCollection.ToList();

            var errors = validator.ValidateCollection(clients);
            if (errors.Count > 0)
            {
                _logger.LogWarning($"CreateClientCollection validation failed for {errors.Count} field(s).");
                return UnprocessableEntity(errors);
            }

            var result = await _service.ClientService.CreateClientCollectionAsync(clients, ct);

            return CreatedAtRoute("ClientCollection", new { result.ids }, result.clients);
        }

        /// <summary>
        /// Soft-delete a client. Cascade-soft-deletes its ClientFounder links and any founders that
        /// become orphaned (no longer linked to any other client).
        /// </summary>
        /// <param name="id">Client id</param>
        /// <param name="ct">Cancellation token</param>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteClient(Guid id, CancellationToken ct)
        {
            await _service.ClientService.DeleteClientAsync(id, ct);

            return NoContent();
        }

        /// <summary>
        /// Partially update a client via JSON Patch
        /// </summary>
        /// <param name="id">Client id</param>
        /// <param name="patchDoc">JSON Patch document (RFC 6902)</param>
        /// <param name="validator">Validator (resolved from DI)</param>
        /// <param name="ct">Cancellation token</param>
        [HttpPatch("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> PartiallyUpdateClient(
            Guid id,
            [FromBody] JsonPatchDocument<ClientForUpdateDto> patchDoc,
            [FromServices] IValidator<ClientForUpdateDto> validator,
            CancellationToken ct)
        {
            if (patchDoc is null)
            {
                _logger.LogWarning($"PartiallyUpdateClient {id}: patchDoc is null.");
                return BadRequest("patchDoc object sent from client is null.");
            }

            var result = await _service.ClientService.GetClientForPatchAsync(id, trackChanges: true, ct);

            patchDoc.ApplyTo(result.clientToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"PartiallyUpdateClient {id} patch apply failed: {ModelState.FormatErrors()}");
                return UnprocessableEntity(ModelState);
            }

            var valResult = validator.Validate(result.clientToPatch);
            if (!valResult.IsValid)
            {
                _logger.LogWarning($"PartiallyUpdateClient {id} validation failed: {valResult.FormatErrors()}");
                return UnprocessableEntity(valResult.ToDictionary());
            }

            await _service.ClientService.SaveChangesForPatchAsync(result.clientToPatch, result.clientEntity, ct);

            return NoContent();
        }
    }
}