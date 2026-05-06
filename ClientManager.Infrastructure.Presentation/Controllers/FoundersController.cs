using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Presentation.Validators;
using FluentValidation;
using LoggingService;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Shared.DataTransferObjects.Founders;

namespace ClientManager.Infrastructure.Presentation.Controllers
{
    [Route("api/clients/{clientId}/founders")]
    [ApiController]
    [ApiExplorerSettings(GroupName = "v1")]
    public class FoundersController : ControllerBase
    {
        private readonly IServiceManager _service;
        private readonly ILoggerManager _logger;

        public FoundersController(IServiceManager service, ILoggerManager logger)
        {
            _service = service;
            _logger = logger;
        }

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
            {
                _logger.LogWarning($"CreateFounderForClient {clientId}: request body is null.");
                return BadRequest("FounderForCreationDto object is null");
            }

            var valResult = validator.Validate(founder);
            if (!valResult.IsValid)
            {
                _logger.LogWarning($"CreateFounderForClient {clientId} validation failed: {valResult.FormatErrors()}");
                return UnprocessableEntity(valResult.ToDictionary());
            }

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
        public async Task<IActionResult> PartiallyUpdateFounderForClient(
            Guid clientId,
            Guid id,
            [FromBody] JsonPatchDocument<FounderForUpdateDto> patchDoc,
            [FromServices] IValidator<FounderForUpdateDto> validator,
            CancellationToken ct)
        {
            if (patchDoc is null)
            {
                _logger.LogWarning($"PartiallyUpdateFounderForClient {clientId}/{id}: patchDoc is null.");
                return BadRequest("patchDoc object sent from client is null.");
            }

            var result = await _service.FounderService.GetFounderForPatchAsync(clientId, id, clientTrackChanges: false, founderTrackChanges: true, ct);

            patchDoc.ApplyTo(result.founderToPatch, ModelState);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"PartiallyUpdateFounderForClient {clientId}/{id} patch apply failed: {ModelState.FormatErrors()}");
                return UnprocessableEntity(ModelState);
            }

            var valResult = validator.Validate(result.founderToPatch);
            if (!valResult.IsValid)
            {
                _logger.LogWarning($"PartiallyUpdateFounderForClient {clientId}/{id} validation failed: {valResult.FormatErrors()}");
                return UnprocessableEntity(valResult.ToDictionary());
            }

            await _service.FounderService.SaveChangesForPatchAsync(result.founderToPatch, result.founderEntity);

            return NoContent();
        }
    }
}