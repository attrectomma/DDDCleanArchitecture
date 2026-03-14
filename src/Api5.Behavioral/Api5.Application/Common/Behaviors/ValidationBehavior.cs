using FluentValidation;
using MediatR;

namespace Api5.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators
/// before the handler executes.
/// </summary>
/// <remarks>
/// DESIGN: This replaces the manual validation calls in service methods
/// (API 1–4) and the ASP.NET FluentValidation auto-validation on request
/// DTOs. Every command/query automatically gets validated if a matching
/// <see cref="IValidator{T}"/> is registered. This is the Open/Closed
/// Principle in action — new validators are discovered by convention
/// without modifying the pipeline or the handler.
///
/// Order: Runs AFTER <see cref="LoggingBehavior{TRequest, TResponse}"/>
/// (so the request is logged even if validation fails) and BEFORE
/// <c>TransactionBehavior</c> (so we never open a transaction for
/// an invalid request).
/// </remarks>
/// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
/// <typeparam name="TResponse">The type of the MediatR response.</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationBehavior{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="validators">All registered validators for the request type.</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Validates the request and throws <see cref="ValidationException"/>
    /// if any rules fail. Otherwise, passes the request to the next behavior.
    /// </summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="next">The next behavior or handler in the pipeline.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="ValidationException">
    /// Thrown when one or more validation rules fail.
    /// </exception>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        List<FluentValidation.Results.ValidationFailure> failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}
