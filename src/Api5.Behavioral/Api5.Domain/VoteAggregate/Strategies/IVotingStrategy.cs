using Api5.Domain.Common.Specifications;

namespace Api5.Domain.VoteAggregate.Strategies;

/// <summary>
/// Defines the contract for a voting strategy that determines how votes
/// are validated before being cast on a retro board.
/// </summary>
/// <remarks>
/// DESIGN: The Strategy pattern encapsulates a family of algorithms (voting
/// rules) behind a common interface. Each retro board has a
/// <see cref="VotingStrategyType"/> that maps to a concrete implementation
/// via <see cref="VotingStrategyFactory"/>.
///
/// The strategy has two responsibilities:
/// <list type="number">
///   <item><see cref="Rules"/>: Exposes the composite specification so callers
///   can inspect or reuse the combined rule (e.g., for bulk eligibility checks
///   in query handlers).</item>
///   <item><see cref="Validate"/>: Checks individual specifications and throws
///   targeted domain exceptions with specific error messages for each failure.</item>
/// </list>
///
/// Compare with API 4 where voting rules were hardcoded in
/// <c>VoteService.CastVoteAsync</c>. Adding a new voting mode would have
/// required modifying that method (violating Open/Closed). With the Strategy
/// pattern, new voting modes are new classes — no existing code changes.
/// </remarks>
public interface IVotingStrategy
{
    /// <summary>
    /// Gets the composite specification representing all voting rules for this strategy.
    /// </summary>
    /// <remarks>
    /// DESIGN: This property exposes the combined rule as a single
    /// <see cref="ISpecification{T}"/> composed via AND/OR/NOT. It can be
    /// used for quick boolean eligibility checks without detailed error messages.
    /// </remarks>
    ISpecification<VoteEligibilityContext> Rules { get; }

    /// <summary>
    /// Validates the vote eligibility context against this strategy's rules.
    /// Throws a targeted domain exception if any rule is violated.
    /// </summary>
    /// <param name="context">The pre-built vote eligibility context.</param>
    /// <exception cref="Exceptions.DomainException">
    /// Thrown when a prerequisite is not met (e.g., note does not exist).
    /// </exception>
    /// <exception cref="Exceptions.InvariantViolationException">
    /// Thrown when a business rule is violated (e.g., duplicate vote, budget exceeded).
    /// </exception>
    void Validate(VoteEligibilityContext context);
}
