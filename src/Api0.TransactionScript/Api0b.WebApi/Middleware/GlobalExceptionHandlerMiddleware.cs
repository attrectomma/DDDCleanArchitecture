using System.Net;
using System.Text.Json;
using Api0b.WebApi.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api0b.WebApi.Middleware;

/// <summary>
/// Global exception handler middleware that catches known application exceptions
/// AND database concurrency/constraint exceptions, returning Problem Details
/// (RFC 7807) responses.
/// </summary>
/// <remarks>
/// DESIGN: This is the critical difference between Api0a and Api0b. Api0a's
/// version of this middleware only catches application-level exceptions
/// (<c>NotFoundException</c>, <c>DuplicateException</c>, <c>BusinessRuleException</c>).
/// Api0b adds two additional catch blocks:
///
/// 1. <c>DbUpdateConcurrencyException</c> → 409 Conflict
///    Triggered when an xmin concurrency token mismatch is detected on
///    User, Project, or RetroBoard entities.
///
/// 2. <c>DbUpdateException</c> with PostgreSQL error 23505 → 409 Conflict
///    Triggered when a unique constraint violation occurs (e.g., concurrent
///    duplicate column names, duplicate votes).
///
/// These ~20 lines of middleware code are what make the concurrency tests
/// pass — without changing a single line of endpoint handler code. Compare
/// with the DDD path where API 3 needed aggregate roots, per-aggregate
/// repositories, full graph loading, and rich domain methods to achieve the
/// same observable HTTP behavior.
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
        catch (DuplicateException ex)
        {
            _logger.LogWarning(ex, "Duplicate detected: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict, "Conflict", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict, "Business Rule Violation", ex.Message);
        }
        // DESIGN: These two catch blocks are the ONLY new code vs. Api0a.
        // They convert database-level concurrency failures into proper
        // HTTP 409 responses instead of letting them bubble up as 500s.
        catch (DbUpdateConcurrencyException ex)
        {
            // DESIGN: Triggered when an xmin concurrency token mismatch is
            // detected. The row was modified by another transaction between
            // our read and our write. The client should retry.
            _logger.LogWarning(ex, "Concurrency conflict: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict, "Concurrency Conflict",
                "The resource was modified by another request. Please retry.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // DESIGN: Triggered when a unique constraint violation occurs at
            // the database level (PostgreSQL error code 23505). This catches
            // race conditions where two concurrent requests both pass the
            // application-level uniqueness check and try to INSERT. The DB
            // unique index prevents the duplicate, and this middleware converts
            // the resulting exception into a meaningful 409 Conflict response.
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
