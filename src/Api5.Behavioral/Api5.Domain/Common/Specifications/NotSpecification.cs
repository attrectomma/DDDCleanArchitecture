namespace Api5.Domain.Common.Specifications;

/// <summary>
/// Composite specification that negates the inner specification (logical NOT).
/// </summary>
/// <typeparam name="T">The type of the candidate to evaluate.</typeparam>
/// <remarks>
/// DESIGN: The NOT composite inverts any specification. For example,
/// <c>new NotSpecification(alreadyVoted)</c> is equivalent to
/// "the user has NOT already voted." Combined with AND and OR, the three
/// boolean combinators provide full expressiveness for rule composition.
/// </remarks>
public class NotSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _inner;

    /// <summary>
    /// Initializes a new NOT composite specification.
    /// </summary>
    /// <param name="inner">The specification to negate.</param>
    public NotSpecification(ISpecification<T> inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <inheritdoc />
    public bool IsSatisfiedBy(T candidate) =>
        !_inner.IsSatisfiedBy(candidate);
}
