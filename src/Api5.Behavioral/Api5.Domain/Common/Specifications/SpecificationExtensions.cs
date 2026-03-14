namespace Api5.Domain.Common.Specifications;

/// <summary>
/// Fluent extension methods for composing specifications using boolean algebra.
/// </summary>
/// <remarks>
/// DESIGN: Extension methods enable a natural, readable syntax for combining
/// specifications:
/// <code>
///   var rule = noteExists.And(isMember).And(uniqueVote);
///   var alternative = isMember.Or(isGuestVoter);
///   var inverse = alreadyVoted.Not();
/// </code>
///
/// Under the hood, these create <see cref="AndSpecification{T}"/>,
/// <see cref="OrSpecification{T}"/>, and <see cref="NotSpecification{T}"/>
/// composites. The resulting tree can be evaluated with a single
/// <see cref="ISpecification{T}.IsSatisfiedBy"/> call.
/// </remarks>
public static class SpecificationExtensions
{
    /// <summary>
    /// Combines this specification with another using logical AND.
    /// Both must be satisfied for the result to be satisfied.
    /// </summary>
    /// <typeparam name="T">The candidate type.</typeparam>
    /// <param name="left">The left-hand specification.</param>
    /// <param name="right">The right-hand specification.</param>
    /// <returns>A composite AND specification.</returns>
    public static ISpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right) =>
        new AndSpecification<T>(left, right);

    /// <summary>
    /// Combines this specification with another using logical OR.
    /// At least one must be satisfied for the result to be satisfied.
    /// </summary>
    /// <typeparam name="T">The candidate type.</typeparam>
    /// <param name="left">The left-hand specification.</param>
    /// <param name="right">The right-hand specification.</param>
    /// <returns>A composite OR specification.</returns>
    public static ISpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) =>
        new OrSpecification<T>(left, right);

    /// <summary>
    /// Negates this specification using logical NOT.
    /// The result is satisfied when this specification is NOT satisfied.
    /// </summary>
    /// <typeparam name="T">The candidate type.</typeparam>
    /// <param name="specification">The specification to negate.</param>
    /// <returns>A composite NOT specification.</returns>
    public static ISpecification<T> Not<T>(this ISpecification<T> specification) =>
        new NotSpecification<T>(specification);
}
