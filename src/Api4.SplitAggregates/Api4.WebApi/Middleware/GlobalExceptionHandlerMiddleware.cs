using System.Net;
using System.Text.Json;
using Api4.Application.Exceptions;
using Api4.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api4.WebApi.Middleware;

/// <summary>
/// Global exception handler middleware that catches known application and
/// domain exceptions and returns Problem Details (RFC 7807) responses.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 3, with an important nuance for API 4: the unique
/// constraint violation handler is now MORE important because Vote is a
/// separate aggregate. The "one vote per user per note" invariant relies on
/// the DB unique constraint as the ultimate safety net (not just defence-in-depth).
/// The VoteService does an application-level check first, but under high
/// concurrency the DB constraint is the real guarantee.
/// </remarks>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="GlobalExceptionHandlerMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware. Catches known exceptions and writes Problem Details.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Entity not found: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, HttpStatusCode.NotFound, "Not Found", ex.Message);
        }
        catch (InvariantViolationException ex)
        {
            _logger.LogWarning(ex, "Invariant violation: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict, "Business Rule Violation", ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict, "Domain Error", ex.Message);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // DESIGN: In API 4 this handler is critical for Vote uniqueness.
            // The VoteService checks for existing votes before creating a new one,
            // but under high concurrency two requests could both pass the check.
            // The DB unique constraint on (NoteId, UserId) catches the race.
            // This middleware converts the DbUpdateException to a 409 Conflict
            // so the client gets a meaningful error instead of a 500.
            _logger.LogWarning(ex, "Unique constraint violation: {Message}", ex.InnerException?.Message ?? ex.Message);
            await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict, "Duplicate Detected",
                "A duplicate entry was detected. The operation conflicts with an existing record.");
        }
    }

    /// <summary>
    /// Writes a Problem Details JSON response to the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="statusCode">The HTTP status code to return.</param>
    /// <param name="title">The problem title.</param>
    /// <param name="detail">The problem detail message.</param>
    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Determines whether the <see cref="DbUpdateException"/> wraps a PostgreSQL
    /// unique constraint violation (error code <c>23505</c>).
    /// </summary>
    /// <param name="ex">The database update exception to inspect.</param>
    /// <returns><c>true</c> if the inner exception is a unique violation; otherwise <c>false</c>.</returns>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation;
}
