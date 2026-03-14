namespace Api5.Domain.Common.Specifications;

/// <summary>
/// Composite specification that requires EITHER the left or right specification
/// to be satisfied (logical OR).
/// </summary>
/// <typeparam name="T">The type of the candidate to evaluate.</typeparam>
/// <remarks>
/// DESIGN: The OR composite enables alternative rules. For example, a future
/// voting strategy could allow a vote if the user is a project member OR has
/// a special "guest voter" role: <c>isMember.Or(isGuestVoter)</c>.
/// The evaluation short-circuits on the first success (left-to-right).
/// </remarks>
public class OrSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    /// <summary>
    /// Initializes a new OR composite specification.
    /// </summary>
    /// <param name="left">The left-hand specification.</param>
    /// <param name="right">The right-hand specification.</param>
    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left ?? throw new ArgumentNullException(nameof(left));
        _right = right ?? throw new ArgumentNullException(nameof(right));
    }

    /// <inheritdoc />
    /// <remarks>Short-circuits: returns <c>true</c> immediately if the left specification passes.</remarks>
    public bool IsSatisfiedBy(T candidate) =>
        _left.IsSatisfiedBy(candidate) || _right.IsSatisfiedBy(candidate);
}
