using System.Net;
using System.Text.Json;
using Api3.Application.Exceptions;
using Api3.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api3.WebApi.Middleware;

/// <summary>
/// Global exception handler middleware that catches known application and
/// domain exceptions and returns Problem Details (RFC 7807) responses.
/// </summary>
/// <remarks>
/// DESIGN: In API 3, this middleware handles Application-layer exceptions
/// (<see cref="NotFoundException"/>) and Domain-layer exceptions
/// (<see cref="InvariantViolationException"/>, <see cref="DomainException"/>).
/// There is no more <c>DuplicateException</c> — all uniqueness checks are
/// inside the aggregate root, which throws <see cref="InvariantViolationException"/>.
///
/// It also catches <see cref="DbUpdateException"/> wrapping PostgreSQL unique
/// constraint violations (error code 23505). These occur when two concurrent
/// requests both pass the in-memory duplicate check in the aggregate root
/// but then collide at the database level. The unique index acts as a
/// defence-in-depth safety net.
///
/// Compare with API 2 which still had DuplicateException for cross-entity
/// uniqueness checks that lived in the service layer.
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
            // DESIGN: This catches PostgreSQL unique constraint violations (error
            // code 23505) that slip through the aggregate's in-memory checks during
            // concurrent requests. The unique indexes on (NoteId, UserId) for votes,
            // (RetroBoardId, Name) for columns, and (ColumnId, Text) for notes act
            // as database-level safety nets. When two concurrent requests both pass
            // the aggregate's in-memory duplicate check, the second INSERT hits the
            // unique constraint and raises DbUpdateException. We convert it to 409
            // Conflict so the client sees a meaningful response.
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
