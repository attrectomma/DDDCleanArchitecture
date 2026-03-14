using MediatR;
using Microsoft.Extensions.Logging;

namespace Api5.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs every command/query entering
/// and leaving the pipeline.
/// </summary>
/// <remarks>
/// DESIGN: In API 1–4, logging was either absent or added ad-hoc in
/// service methods. Here, every command and query automatically gets
/// logged by being in the MediatR pipeline. This is the power of
/// cross-cutting concerns as pipeline behaviors — new behaviors apply
/// to ALL requests without modifying individual handlers.
///
/// Order: This behavior runs FIRST in the pipeline (registered before
/// ValidationBehavior and TransactionBehavior) so it captures the raw
/// request before validation rejects it.
/// </remarks>
/// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
/// <typeparam name="TResponse">The type of the MediatR response.</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="LoggingBehavior{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the request name before and after handler execution.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the handler.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        string requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}: {@Request}", requestName, request);

        TResponse response = await next();

        _logger.LogInformation("Handled {RequestName}", requestName);
        return response;
    }
}
