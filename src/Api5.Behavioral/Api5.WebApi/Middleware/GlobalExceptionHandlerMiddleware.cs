using System.Net;
using System.Text.Json;
using Api5.Application.Common.Exceptions;
using Api5.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api5.WebApi.Middleware;

/// <summary>
/// Global exception handler middleware that catches known application,
/// domain, and validation exceptions and returns Problem Details
/// (RFC 7807) responses.
/// </summary>
/// <remarks>
/// DESIGN: Same structure as API 3/4 but with an added handler for
/// <see cref="ValidationException"/> thrown by the MediatR
/// <c>ValidationBehavior</c> pipeline. In API 4, FluentValidation
/// auto-validation (ASP.NET model binding) handled this before the
/// request even reached the service layer. In API 5, validation
/// happens inside the MediatR pipeline, so the exception bubbles up
/// to this middleware.
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
        catch (ValidationException ex)
        {
            // DESIGN: NEW in API 5. The ValidationBehavior pipeline throws
            // FluentValidation.ValidationException when command/query validation
            // fails. We map this to 400 Bad Request with validation error details.
            _logger.LogWarning(ex, "Validation failed: {Message}", ex.Message);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/problem+json";

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };

            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
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
