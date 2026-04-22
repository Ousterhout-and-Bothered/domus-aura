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
            ResourceNotFoundException => Build(
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message,
                "https://domus-aura.com/problems/resource-not-found",
                httpContext),

            DuplicateThermostatException => Build(
                StatusCodes.Status409Conflict,
                "Duplicate thermostat",
                exception.Message,
                "https://domus-aura.com/problems/duplicate-thermostat",
                httpContext),

            InvalidDomainArgumentException => Build(
                StatusCodes.Status400BadRequest,
                "Invalid domain argument",
                exception.Message,
                "https://domus-aura.com/problems/invalid-request",
                httpContext),

            InvalidDomainOperationException => Build(
                StatusCodes.Status400BadRequest,
                "Invalid domain operation",
                exception.Message,
                "https://domus-aura.com/problems/invalid-operation",
                httpContext),

            DomainException => Build(
                StatusCodes.Status400BadRequest,
                "Domain error",
                exception.Message,
                "https://domus-aura.com/problems/domain-error",
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