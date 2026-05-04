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
        public IActionResult GetClients()
        {
            var clients = _service.ClientService.GetAllClients(trackChanges: false);

            return Ok(clients);
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetClient(Guid id)
        {
            var client = _service.ClientService.GetClient(id, trackChanges: false);

            return Ok(client);
        }
    }
}