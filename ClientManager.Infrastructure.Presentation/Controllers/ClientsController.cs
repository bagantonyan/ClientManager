using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Presentation.ModelBinders;
using ClientManager.Infrastructure.Presentation.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.DataTransferObjects.Clients;
using Shared.RequestFeatures;
using System.Text.Json;

namespace ClientManager.Infrastructure.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    public class ClientsController : ControllerBase
    {
        private readonly IServiceManager _service;

        public ClientsController(IServiceManager service) => _service = service;

        /// <summary>
        /// Get all clients
        /// </summary>
        /// <returns>The clients list</returns>
        [HttpGet]
        [EnableRateLimiting("SpecificPolicy")]
        public async Task<IActionResult> GetClients(
            [FromQuery] ClientParameters clientParameters, 
            CancellationToken ct, 
            [FromQuery] bool includeFounders = true)
        {
            var pagedResult = await _service.ClientService.GetAllClientsAsync(clientParameters, trackChanges: false, includeFounders, ct);

            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagedResult.metaData));

            return Ok(pagedResult.clients);
        }

        [HttpGet("{id:guid}", Name = "ClientById")]
        [DisableRateLimiting]
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
                return BadRequest("ClientForCreationDto object is null");

            var valResult = validator.Validate(client);
            if (!valResult.IsValid)
                return UnprocessableEntity(valResult.ToDictionary());

            var createdClient = await _service.ClientService.CreateClientAsync(client, ct);

            return CreatedAtRoute("ClientById", new { id = createdClient.Id }, createdClient);
        }

        [HttpGet("collection/({ids})", Name = "ClientCollection")]
        public async Task<IActionResult> GetClientCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids,
            CancellationToken ct,
            [FromQuery] bool includeFounders = true)
        {
            var clients = await _service.ClientService.GetByIdsAsync(ids, trackChanges: false, includeFounders, ct);

            return Ok(clients);
        }

        [HttpPost("collection")]
        public async Task<IActionResult> CreateClientCollection(
            [FromBody] IEnumerable<ClientForCreationDto> clientCollection,
            [FromServices] IValidator<ClientForCreationDto> validator,
            CancellationToken ct)
        {
            if (clientCollection is null)
                return BadRequest("ClientCollection object is null");

            var clients = clientCollection.ToList();

            var errors = validator.ValidateCollection(clients);
            if (errors.Count > 0)
                return UnprocessableEntity(errors);

            var result = await _service.ClientService.CreateClientCollectionAsync(clients, ct);

            return CreatedAtRoute("ClientCollection", new { result.ids }, result.clients);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteClient(Guid id, CancellationToken ct)
        {
            await _service.ClientService.DeleteClientAsync(id, ct);

            return NoContent();
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> PartiallyUpdateClient(
            Guid id,
            [FromBody] JsonPatchDocument<ClientForUpdateDto> patchDoc,
            [FromServices] IValidator<ClientForUpdateDto> validator,
            CancellationToken ct)
        {
            if (patchDoc is null)
                return BadRequest("patchDoc object sent from client is null.");

            var result = await _service.ClientService.GetClientForPatchAsync(id, trackChanges: true, ct);

            patchDoc.ApplyTo(result.clientToPatch, ModelState);

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            var valResult = validator.Validate(result.clientToPatch);
            if (!valResult.IsValid)
                return UnprocessableEntity(valResult.ToDictionary());

            await _service.ClientService.SaveChangesForPatchAsync(result.clientToPatch, result.clientEntity, ct);

            return NoContent();
        }
    }
}