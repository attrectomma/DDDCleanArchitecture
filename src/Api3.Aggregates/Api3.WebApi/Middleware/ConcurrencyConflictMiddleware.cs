using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api3.WebApi.Middleware;

/// <summary>
/// Middleware that catches <see cref="DbUpdateConcurrencyException"/>
/// and returns HTTP 409 Conflict with a meaningful Problem Details message.
/// </summary>
/// <remarks>
/// DESIGN: This middleware is NEW in API 3. APIs 1 and 2 did not have
/// optimistic concurrency, so this exception never occurred. With aggregate
/// roots using <c>UseXminAsConcurrencyToken()</c>, concurrent writes to the
/// same aggregate cause <see cref="DbUpdateConcurrencyException"/>.
///
/// This middleware converts that exception into a 409 Conflict response
/// with a clear message telling the client to retry. In a production
/// system, you might implement automatic retry logic; here we keep it
/// simple for educational purposes.
/// </remarks>
public class ConcurrencyConflictMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ConcurrencyConflictMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ConcurrencyConflictMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ConcurrencyConflictMiddleware(RequestDelegate next, ILogger<ConcurrencyConflictMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware. Catches concurrency exceptions and writes Problem Details.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected: {Message}", ex.Message);

            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = 409,
                Title = "Concurrency conflict",
                Detail = "The resource was modified by another request. Please retry.",
                Instance = context.Request.Path
            };

            var json = System.Text.Json.JsonSerializer.Serialize(problemDetails,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });

            await context.Response.WriteAsync(json);
        }
    }
}
