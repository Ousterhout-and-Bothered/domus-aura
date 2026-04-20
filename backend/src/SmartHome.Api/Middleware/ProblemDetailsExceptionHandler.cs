using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SmartHome.Api.Middleware;

public sealed class ProblemDetailsExceptionHandler(
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Log full exception server-side — never leak details to clients.
        logger.LogError(exception, "Unhandled exception processing {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var problem = MapToProblemDetails(exception, httpContext);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        // Returning true signals "handled — don't propagate further".
        return true;
    }

    private static ProblemDetails MapToProblemDetails(Exception exception, HttpContext httpContext) =>
        exception switch
        {
            ArgumentOutOfRangeException => Build(
                StatusCodes.Status400BadRequest,
                "Invalid argument",
                exception.Message,
                httpContext),

            ArgumentException => Build(
                StatusCodes.Status400BadRequest,
                "Invalid input",
                exception.Message,
                httpContext),

            InvalidOperationException => Build(
                StatusCodes.Status409Conflict,
                "Invalid operation",
                exception.Message,
                httpContext),

            KeyNotFoundException => Build(
                StatusCodes.Status404NotFound,
                "Resource not found",
                exception.Message,
                httpContext),

            _ => Build(
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                // Deliberately generic — don't leak internals to the client.
                "An unexpected error occurred while processing your request.",
                httpContext)
        };

    private static ProblemDetails Build(int status, string title, string detail, HttpContext httpContext) =>
        new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = $"https://tools.ietf.org/html/rfc9110#section-15.{(status / 100)}"
        };
}