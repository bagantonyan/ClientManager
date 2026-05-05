using ClientManager.Core.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Shared.DataTransferObjects.Founders;

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

        [HttpGet("{id:guid}", Name = "GetFounderForClient")]
        public IActionResult GetFounderForClient(Guid clientId, Guid id)
        {
            var founder = _service.FounderService.GetFounder(clientId, id, trackChanges: false);

            return Ok(founder);
        }

        [HttpPost]
        public IActionResult CreateFounderForClient(Guid clientId, [FromBody] FounderForCreationDto founder)
        {
            if (founder is null)
                return BadRequest("FounderForCreationDto object is null");

            var founderToReturn = _service.FounderService.CreateFounderForClient(clientId, founder, trackChanges: true);

            return CreatedAtRoute("GetEmployeeForCompany", new { clientId, id = founderToReturn.Id }, founderToReturn);
        }
    }
}