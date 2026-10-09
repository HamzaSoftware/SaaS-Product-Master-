using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure;

public sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed."),
            ResourceNotFoundException => (StatusCodes.Status404NotFound, "Resource not found."),
            ConflictException or DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };
        if (status == StatusCodes.Status500InternalServerError) LogUnhandledException(logger, exception, httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status < 500 || environment.IsDevelopment() ? exception.Message : null,
                Type = $"https://httpstatuses.com/{status}",
                Extensions = { ["correlationId"] = httpContext.TraceIdentifier }
            },
            Exception = exception
        });
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled request exception for {TraceIdentifier}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string traceIdentifier);
}
