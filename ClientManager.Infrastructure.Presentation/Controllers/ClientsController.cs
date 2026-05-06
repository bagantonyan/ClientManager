using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Presentation.ModelBinders;
using ClientManager.Infrastructure.Presentation.Validators;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Shared.DataTransferObjects.Clients;

namespace ClientManager.Infrastructure.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IServiceManager _service;

        public ClientsController(IServiceManager service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetClients(CancellationToken ct, [FromQuery] bool includeFounders = true)
        {
            var clients = await _service.ClientService.GetAllClientsAsync(trackChanges: false, includeFounders, ct);

            return Ok(clients);
        }

        [HttpGet("{id:guid}", Name = "ClientById")]
        public async Task<IActionResult> GetClient(Guid id, CancellationToken ct, [FromQuery] bool includeFounders = true)
        {
            var client = await _service.ClientService.GetClientAsync(id, trackChanges: false, includeFounders, ct);

            return Ok(client);
        }

        [HttpPost]
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