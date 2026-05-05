using ClientManager.Core.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ClientManager.Infrastructure.Presentation.Controllers
{
    [Route("api/clients/{clientId}/founders")]
    [ApiController]
    public class FoundersController : ControllerBase
    {
        private readonly IServiceManager _service;

        public FoundersController(IServiceManager service) => _service = service;

        [HttpGet]
        public IActionResult GetFoundersForClient(Guid clientId)
        {
            var founders = _service.FounderService.GetFounders(clientId, trackChanges: false);

            return Ok(founders);
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetFounderForClient(Guid clientId, Guid id)
        {
            var founder = _service.FounderService.GetFounder(clientId, id, trackChanges: false);

            return Ok(founder);
        }
    }
}