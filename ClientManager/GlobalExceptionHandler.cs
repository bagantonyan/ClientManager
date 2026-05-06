using ClientManager.Core.Domain.Exceptions;
using LoggingService;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClientManager
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILoggerManager _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public GlobalExceptionHandler(ILoggerManager logger, IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            httpContext.Response.ContentType = "application/json";

            var (statusCode, title) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            httpContext.Response.StatusCode = statusCode;

            var logMessage = $"Request to {httpContext.Request.Method} {httpContext.Request.Path} failed with {statusCode}: {exception.Message}";

            if (statusCode >= 500)
                _logger.LogError(logMessage);
            else
                _logger.LogWarning(logMessage);

            var result = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails =
                {
                    Title = title,
                    Status = statusCode,
                    Detail = exception.Message,
                    Type = exception.GetType().Name
                },
                Exception = exception
            });

            if (!result)
                await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Title = title,
                    Status = statusCode,
                    Detail = exception.Message,
                    Type = exception.GetType().Name
                });

            return true;
        }
    }
}