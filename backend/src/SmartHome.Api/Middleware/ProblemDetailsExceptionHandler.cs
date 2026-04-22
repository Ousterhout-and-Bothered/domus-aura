using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Domain.Common.Exceptions;

namespace SmartHome.Api.Middleware;

public sealed class ProblemDetailsExceptionHandler(
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = MapToProblemDetails(exception, httpContext);
        var statusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, 
                "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception,
                "Handled exception mapped to {StatusCode} for {Method} {Path}",
                statusCode, httpContext.Request.Method, httpContext.Request.Path);
        }
        
        if (httpContext.Response.HasStarted)
            return false;
        
        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        // Returning true signals "handled — don't propagate further".
        return true;
    }

    private static ProblemDetails MapToProblemDetails(Exception exception, HttpContext httpContext) =>
        exception switch
        {
            DuplicateThermostatException => Build(
                StatusCodes.Status409Conflict,
                "Duplicate thermostat",
                exception.Message,
                "https://domus-aura.com/problems/duplicate-thermostat",
                httpContext),

            ArgumentOutOfRangeException => Build(
                StatusCodes.Status400BadRequest,
                "Invalid argument",
                exception.Message,
                "https://domus-aura.com/problems/invalid-request",
                httpContext),

            ArgumentException => Build(
                StatusCodes.Status400BadRequest,
                "Invalid input",
                exception.Message,
                "https://domus-aura.com/problems/invalid-request",
                httpContext),

            InvalidOperationException => Build(
                StatusCodes.Status400BadRequest,
                "Invalid operation",
                exception.Message,
                "https://domus-aura.com/problems/invalid-operation",
                httpContext),

            KeyNotFoundException => Build(
                StatusCodes.Status404NotFound,
                "Device not found",
                exception.Message,
                "https://domus-aura.com/problems/device-not-found",
                httpContext),

            _ => Build(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                // Deliberately generic — don't leak internals to the client.
                "An unexpected error occurred while processing your request.",
                null,
                httpContext)
        };

    private static ProblemDetails Build(int status, string title, string detail, string? type, HttpContext httpContext) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = type ?? $"https://www.rfc-editor.org/rfc/rfc9110#section-15.{(status / 100) + 1}"
        };
}