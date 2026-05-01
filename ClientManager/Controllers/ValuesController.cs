using LoggingService;
using Microsoft.AspNetCore.Mvc;

namespace ClientManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly ILoggerManager _logger;
        public ValuesController(ILoggerManager logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Test()
        {
            _logger.LogDebug("This is a debug message.");
            _logger.LogInformation("This is an information.");
            _logger.LogWarning("This is a warn message.");
            _logger.LogError("This is an error message.");

            return Ok();
        }
    }
}
