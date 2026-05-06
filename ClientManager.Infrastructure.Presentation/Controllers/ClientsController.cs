using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Presentation.ModelBinders;
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
        public async Task<IActionResult> GetClients([FromQuery] bool includeFounders = true)
        {
            var clients = await _service.ClientService.GetAllClientsAsync(trackChanges: false, includeFounders);

            return Ok(clients);
        }

        [HttpGet("{id:guid}", Name = "ClientById")]
        public async Task<IActionResult> GetClient(Guid id, [FromQuery] bool includeFounders = true)
        {
            var client = await _service.ClientService.GetClientAsync(id, trackChanges: false, includeFounders);

            return Ok(client);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] ClientForCreationDto client)
        {
            if (client is null)
                return BadRequest("ClientForCreationDto object is null");

            var createdClient = await _service.ClientService.CreateClientAsync(client);

            return CreatedAtRoute("ClientById", new { id = createdClient.Id }, createdClient);
        }

        [HttpGet("collection/({ids})", Name = "ClientCollection")]
        public async Task<IActionResult> GetClientCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids,
            [FromQuery] bool includeFounders = true)
        {
            var clients = await _service.ClientService.GetByIdsAsync(ids, trackChanges: false, includeFounders);

            return Ok(clients);
        }

        [HttpPost("collection")]
        public async Task<IActionResult> CreateClientCollection([FromBody] IEnumerable<ClientForCreationDto> clientCollection)
        {
            var result = await _service.ClientService.CreateClientCollectionAsync(clientCollection);

            return CreatedAtRoute("ClientCollection", new { result.ids }, result.clients);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteClient(Guid id)
        {
            await _service.ClientService.DeleteClientAsync(id);

            return NoContent();
        }
    }
}