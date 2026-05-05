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
        public IActionResult GetClients([FromQuery] bool includeFounders = true)
        {
            var clients = _service.ClientService.GetAllClients(trackChanges: false, includeFounders);

            return Ok(clients);
        }

        [HttpGet("{id:guid}", Name = "ClientById")]
        public IActionResult GetClient(Guid id, [FromQuery] bool includeFounders = true)
        {
            var client = _service.ClientService.GetClient(id, trackChanges: false, includeFounders);

            return Ok(client);
        }

        [HttpPost]
        public IActionResult CreateClient([FromBody] ClientForCreationDto client)
        {
            if (client is null)
                return BadRequest("ClientForCreationDto object is null");

            var createdClient = _service.ClientService.CreateClient(client);

            return CreatedAtRoute("ClientById", new { id = createdClient.Id }, createdClient);
        }

        [HttpGet("collection/({ids})", Name = "ClientCollection")]
        public IActionResult GetClientCollection(
            [ModelBinder(BinderType = typeof(ArrayModelBinder))] IEnumerable<Guid> ids,
            [FromQuery] bool includeFounders = true)
        {
            var clients = _service.ClientService.GetByIds(ids, trackChanges: false, includeFounders);

            return Ok(clients);
        }

        [HttpPost("collection")]
        public IActionResult CreateClientCollection([FromBody] IEnumerable<ClientForCreationDto> clientCollection)
        {
            var result = _service.ClientService.CreateClientCollection(clientCollection);

            return CreatedAtRoute("ClientCollection", new { result.ids }, result.clients);
        }
    }
}