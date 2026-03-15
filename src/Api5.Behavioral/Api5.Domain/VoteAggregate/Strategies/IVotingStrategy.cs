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
    /// DESIGN (dual-surface): The strategy exposes two ways to evaluate eligibility:
    /// <list type="bullet">
    ///   <item><see cref="Rules"/> — returns a single <see cref="ISpecification{T}"/> composed
    ///   via AND/OR/NOT. It yields a flat <c>bool</c> and is ideal for scenarios where you
    ///   need to check eligibility <em>without</em> throwing or need to compose the check
    ///   into a larger expression.</item>
    ///   <item><see cref="Validate"/> — evaluates the same individual specifications but
    ///   throws a targeted domain exception on the <em>first</em> failing rule, providing
    ///   a meaningful error message for API consumers.</item>
    /// </list>
    ///
    /// In this codebase, the production command handler (<c>CastVoteCommandHandler</c>)
    /// calls <see cref="Validate"/> because it needs specific error messages. The
    /// <see cref="Rules"/> property is not currently called in production code, but it
    /// exists to demonstrate the Specification pattern's composability and to support
    /// scenarios such as:
    /// <list type="bullet">
    ///   <item><b>Bulk eligibility filtering</b> — a query handler could call
    ///   <c>strategy.Rules.IsSatisfiedBy(ctx)</c> in a loop over many notes to build
    ///   a "can vote" / "cannot vote" UI indicator without throwing per item.</item>
    ///   <item><b>UI pre-flight checks</b> — a front-end-facing endpoint could return
    ///   a boolean eligibility result so the UI can disable the vote button before
    ///   the user even clicks it.</item>
    ///   <item><b>Compound specifications</b> — a new strategy could compose
    ///   <c>existingStrategy.Rules.And(additionalSpec)</c> to extend rules without
    ///   subclassing.</item>
    /// </list>
    ///
    /// The unit tests exercise <see cref="Rules"/> directly to prove that the composite
    /// evaluates correctly, independently from the exception-throwing path.
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
