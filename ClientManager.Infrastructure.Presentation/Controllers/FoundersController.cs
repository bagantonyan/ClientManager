using Asp.Versioning;
using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Presentation.Http;
using ClientManager.Infrastructure.Presentation.Validators;
using FluentValidation;
using LoggingService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Shared.DataTransferObjects.Founders;

namespace ClientManager.Infrastructure.Presentation.Controllers
{
    [ApiVersion("1.0")]
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

        /// <summary>
        /// Get all founders of a client
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The founders list</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFoundersForClient(Guid clientId, CancellationToken ct)
        {
            var founders = await _service.FounderService.GetFoundersAsync(clientId, trackChanges: false, ct);

            return Ok(founders);
        }

        /// <summary>
        /// Get a specific founder of a client
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="id">Founder id</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The founder</returns>
        [HttpGet("{id:guid}", Name = "GetFounderForClient")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFounderForClient(Guid clientId, Guid id, CancellationToken ct)
        {
            var founder = await _service.FounderService.GetFounderAsync(clientId, id, trackChanges: false, ct);

            if (founder.RowVersion is not null)
                Response.Headers.ETag = ETagHelper.ToETag(founder.RowVersion);

            return Ok(founder);
        }

        /// <summary>
        /// Add a founder to a client. If a founder with the same INN exists (active or soft-deleted),
        /// it is reused or restored instead of creating a duplicate.
        /// </summary>
        /// <param name="clientId">Client id (must be of type Legal_Entity)</param>
        /// <param name="founder">Founder data</param>
        /// <param name="validator">Validator (resolved from DI)</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>The newly created or restored founder</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
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

            if (founderToReturn.RowVersion is not null)
                Response.Headers.ETag = ETagHelper.ToETag(founderToReturn.RowVersion);

            return CreatedAtRoute("GetFounderForClient", new { clientId, id = founderToReturn.Id }, founderToReturn);
        }

        /// <summary>
        /// Unlink a founder from a client. If the founder has no other active links, soft-deletes
        /// the founder as well. Removing the last founder of a Legal_Entity is forbidden.
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="id">Founder id</param>
        /// <param name="ct">Cancellation token</param>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFounderForClient(Guid clientId, Guid id, CancellationToken ct)
        {
            await _service.FounderService.DeleteFounderForClientAsync(clientId, id, trackChanges: true, ct);

            return NoContent();
        }

        /// <summary>
        /// Partially update a founder of a client via JSON Patch
        /// </summary>
        /// <param name="clientId">Client id</param>
        /// <param name="id">Founder id</param>
        /// <param name="patchDoc">JSON Patch document (RFC 6902)</param>
        /// <param name="ifMatch">Optional ETag from a previous GET. If provided and stale, 409 Conflict is returned.</param>
        /// <param name="validator">Validator (resolved from DI)</param>
        /// <param name="ct">Cancellation token</param>
        [HttpPatch("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> PartiallyUpdateFounderForClient(
            Guid clientId,
            Guid id,
            [FromBody] JsonPatchDocument<FounderForUpdateDto> patchDoc,
            [FromHeader(Name = "If-Match")] string? ifMatch,
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

            var ifMatchBytes = ETagHelper.TryParseIfMatch(ifMatch);

            await _service.FounderService.SaveChangesForPatchAsync(result.founderToPatch, result.founderEntity, ifMatchBytes, ct);

            Response.Headers.ETag = ETagHelper.ToETag(result.founderEntity.RowVersion);

            return NoContent();
        }
    }
}