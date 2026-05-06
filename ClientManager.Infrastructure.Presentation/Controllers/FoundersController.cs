using ClientManager.Core.Services.Abstractions;
using FluentValidation;
using Microsoft.AspNetCore.JsonPatch;
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
        public async Task<IActionResult> GetFoundersForClient(Guid clientId, CancellationToken ct)
        {
            var founders = await _service.FounderService.GetFoundersAsync(clientId, trackChanges: false, ct);

            return Ok(founders);
        }

        [HttpGet("{id:guid}", Name = "GetFounderForClient")]
        public async Task<IActionResult> GetFounderForClient(Guid clientId, Guid id, CancellationToken ct)
        {
            var founder = await _service.FounderService.GetFounderAsync(clientId, id, trackChanges: false, ct);

            return Ok(founder);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFounderForClient(
            Guid clientId, 
            [FromBody] FounderForCreationDto founder,
            [FromServices] IValidator<FounderForCreationDto> validator,
            CancellationToken ct)
        {
            if (founder is null)
                return BadRequest("FounderForCreationDto object is null");

            var valResult = validator.Validate(founder);
            if (!valResult.IsValid)
                return UnprocessableEntity(valResult.ToDictionary());

            var founderToReturn = await _service.FounderService.CreateFounderForClientAsync(clientId, founder, trackChanges: true, ct);

            return CreatedAtRoute("GetFounderForClient", new { clientId, id = founderToReturn.Id }, founderToReturn);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteFounderForClient(Guid clientId, Guid id, CancellationToken ct)
        {
            await _service.FounderService.DeleteFounderForClientAsync(clientId, id, trackChanges: true, ct);

            return NoContent();
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> PartiallyUpdateFounderForClient(Guid clientId, Guid id, [FromBody] JsonPatchDocument<FounderForUpdateDto> patchDoc, CancellationToken ct)
        {
            if (patchDoc is null)
                return BadRequest("patchDoc object sent from client is null.");

            var result = await _service.FounderService.GetFounderForPatchAsync(clientId, id, clientTrackChanges: false, founderTrackChanges: true, ct);

            patchDoc.ApplyTo(result.founderToPatch, ModelState);

            TryValidateModel(result.founderToPatch);

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            await _service.FounderService.SaveChangesForPatchAsync(result.founderToPatch, result.founderEntity);

            return NoContent();
        }
    }
}