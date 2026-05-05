using ClientManager.Core.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("{id:guid}")]
        public IActionResult GetClient(Guid id, [FromQuery] bool includeFounders = true)
        {
            var client = _service.ClientService.GetClient(id, trackChanges: false, includeFounders);

            return Ok(client);
        }
    }
}