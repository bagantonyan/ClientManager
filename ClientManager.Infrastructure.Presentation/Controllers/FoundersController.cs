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
        public async Task<IActionResult> GetFoundersForClient(Guid clientId)
        {
            var founders = await _service.FounderService.GetFoundersAsync(clientId, trackChanges: false);

            return Ok(founders);
        }

        [HttpGet("{id:guid}", Name = "GetFounderForClient")]
        public async Task<IActionResult> GetFounderForClient(Guid clientId, Guid id)
        {
            var founder = await _service.FounderService.GetFounderAsync(clientId, id, trackChanges: false);

            return Ok(founder);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFounderForClient(Guid clientId, [FromBody] FounderForCreationDto founder)
        {
            if (founder is null)
                return BadRequest("FounderForCreationDto object is null");

            var founderToReturn = await _service.FounderService.CreateFounderForClientAsync(clientId, founder, trackChanges: true);

            return CreatedAtRoute("GetFounderForClient", new { clientId, id = founderToReturn.Id }, founderToReturn);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteFounderForClient(Guid clientId, Guid id)
        {
            await _service.FounderService.DeleteFounderForClientAsync(clientId, id, trackChanges: true);

            return NoContent();
        }
    }
}