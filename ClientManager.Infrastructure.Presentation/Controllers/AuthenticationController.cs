using ClientManager.Core.Services.Abstractions;
using ClientManager.Infrastructure.Presentation.Validators;
using FluentValidation;
using LoggingService;
using Microsoft.AspNetCore.Mvc;
using Shared.DataTransferObjects.Users;

namespace ClientManager.Infrastructure.Presentation.Controllers
{
    [Route("api/authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IServiceManager _service;
        private readonly ILoggerManager _logger;

        public AuthenticationController(
            IServiceManager service,
            ILoggerManager logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> RegisterUser(
            [FromBody] UserForRegistrationDto userForRegistration,
            [FromServices] IValidator<UserForRegistrationDto> validator)
        {
            if (userForRegistration is null)
            {
                _logger.LogWarning("RegisterUser: request body is null.");
                return BadRequest("UserForRegistrationDto object is null");
            }

            var valResult = validator.Validate(userForRegistration);

            if (!valResult.IsValid)
            {
                _logger.LogWarning($"RegisterUser validation failed: {valResult.FormatErrors()}");
                return UnprocessableEntity(valResult.ToDictionary());
            }

            var result = await _service.AuthenticationService.RegisterUser(userForRegistration);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.TryAddModelError(error.Code, error.Description);
                }

                return BadRequest(ModelState);
            }

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Authenticate(
            [FromBody] UserForAuthenticationDto user,
            [FromServices] IValidator<UserForAuthenticationDto> validator)
        {
            if (user is null)
            {
                _logger.LogWarning("Authenticate: request body is null.");
                return BadRequest("UserForAuthenticationDto object is null");
            }

            var valResult = validator.Validate(user);

            if (!valResult.IsValid)
            {
                _logger.LogWarning($"RegisterUser validation failed: {valResult.FormatErrors()}");
                return UnprocessableEntity(valResult.ToDictionary());
            }

            if (!await _service.AuthenticationService.ValidateUser(user))
                return Unauthorized();

            var tokenDto = await _service.AuthenticationService.CreateToken(populateExp: true);

            return Ok(tokenDto);
        }
    }
}