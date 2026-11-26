using Microsoft.AspNetCore.Mvc;

namespace Voltaguard.Api.Middleware;

/// <summary>
/// Turns unhandled exceptions into RFC 7807 ProblemDetails so controllers never
/// hand-build an error shape and no stack trace leaks to the client. Business-rule
/// failures (InvalidOperationException) map to 400; anything else maps to 500.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rejected request to {Path}", context.Request.Path);
            await WriteProblem(context, StatusCodes.Status400BadRequest, "Request could not be processed", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error on {Path}", context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", "Please try again later.");
        }
    }

    private static Task WriteProblem(HttpContext context, int statusCode, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        return context.Response.WriteAsJsonAsync(problem);
    }
}
