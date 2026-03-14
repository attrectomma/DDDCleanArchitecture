namespace Api5.Domain.Common.Specifications;

/// <summary>
/// Represents a single business rule that can evaluate whether a candidate
/// satisfies a condition. Specifications are composable via
/// <see cref="SpecificationExtensions.And{T}"/>, <see cref="SpecificationExtensions.Or{T}"/>,
/// and <see cref="SpecificationExtensions.Not{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the candidate to evaluate.</typeparam>
/// <remarks>
/// DESIGN: The Specification pattern encapsulates business rules as first-class
/// objects. Each specification answers one question: "Does this candidate satisfy
/// the rule?" Specifications can be composed using boolean algebra (AND, OR, NOT)
/// to build complex rules from simple, testable building blocks.
///
/// In API 5, specifications are used by voting strategies to define vote
/// eligibility rules. The <see cref="ISpecification{T}"/> interface keeps the
/// rules in the Domain layer — pure, infrastructure-free, and unit-testable.
///
/// Compare with API 4 where eligibility checks were inline in VoteService
/// methods. With specifications, each rule is isolated, named, and reusable
/// across different strategies.
/// </remarks>
public interface ISpecification<in T>
{
    /// <summary>
    /// Evaluates whether the given candidate satisfies this specification.
    /// </summary>
    /// <param name="candidate">The candidate to evaluate.</param>
    /// <returns><c>true</c> if the candidate satisfies the specification; otherwise <c>false</c>.</returns>
    bool IsSatisfiedBy(T candidate);
}
