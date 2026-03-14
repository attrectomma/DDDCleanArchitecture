namespace Api5.Domain.Common.Specifications;

/// <summary>
/// Composite specification that requires BOTH the left and right specifications
/// to be satisfied (logical AND).
/// </summary>
/// <typeparam name="T">The type of the candidate to evaluate.</typeparam>
/// <remarks>
/// DESIGN: The AND composite is the most common combinator. It enables building
/// rule chains like: <c>noteExists.And(isMember).And(uniqueVote)</c> where all
/// three must pass for the composite to be satisfied. The evaluation short-circuits
/// on the first failure (left-to-right).
/// </remarks>
public class AndSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    /// <summary>
    /// Initializes a new AND composite specification.
    /// </summary>
    /// <param name="left">The left-hand specification.</param>
    /// <param name="right">The right-hand specification.</param>
    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    /// <inheritdoc />
    /// <remarks>Short-circuits: returns <c>false</c> immediately if the left specification fails.</remarks>
    public bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) && _right.IsSatisfiedBy(candidate);
}
