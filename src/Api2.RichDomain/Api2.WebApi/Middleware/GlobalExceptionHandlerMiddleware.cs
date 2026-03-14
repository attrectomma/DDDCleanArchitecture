using System.Net;
using System.Text.Json;
using Api2.Application.Exceptions;
using Api2.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api2.WebApi.Middleware;

/// <summary>
/// Global exception handler middleware that catches known application and
/// domain exceptions and returns Problem Details (RFC 7807) responses.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, this middleware handles both Application-layer exceptions
/// (<see cref="NotFoundException"/>, <see cref="DuplicateException"/>) and
/// Domain-layer exceptions (<see cref="InvariantViolationException"/>,
/// <see cref="DomainException"/>). Domain exceptions are mapped to HTTP 409
/// Conflict to indicate a business rule violation.
///
/// Compare with API 1 where only Application exceptions existed
/// (<c>BusinessRuleException</c> served the same role as <c>InvariantViolationException</c>).
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
}
