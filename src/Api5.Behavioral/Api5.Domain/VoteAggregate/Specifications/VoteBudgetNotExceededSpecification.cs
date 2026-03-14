using Api5.Domain.Common.Specifications;
using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Domain.VoteAggregate.Specifications;

/// <summary>
/// Specification that checks whether the user has remaining votes in the
/// column (i.e., has not exceeded the per-column vote budget).
/// Used exclusively by the <see cref="BudgetVotingStrategy"/>.
/// </summary>
/// <remarks>
/// DESIGN: This specification encapsulates the "dot voting" budget rule.
/// In budget voting, each user gets a fixed number of votes per column
/// (e.g., 3). They can distribute those votes freely — including placing
/// multiple votes on the same note to signal strong agreement.
///
/// The <see cref="MaxVotesPerColumn"/> limit is injected via the constructor,
/// making the specification configurable. The <see cref="DefaultVotingStrategy"/>
/// does NOT use this specification because it has no per-column budget — it
/// uses <see cref="UniqueVotePerNoteSpecification"/> instead.
///
/// This is a great example of how the Specification pattern keeps rules
/// isolated: changing the budget limit only touches this one class.
/// </remarks>
public class VoteBudgetNotExceededSpecification : ISpecification<VoteEligibilityContext>
{
    /// <summary>
    /// Initializes a new instance of <see cref="VoteBudgetNotExceededSpecification"/>
    /// with the specified maximum votes per column.
    /// </summary>
    /// <param name="maxVotesPerColumn">The maximum number of votes a user may cast per column.</param>
    public VoteBudgetNotExceededSpecification(int maxVotesPerColumn)
    {
        MaxVotesPerColumn = maxVotesPerColumn;
    }

    /// <summary>Gets the maximum number of votes a user may cast per column.</summary>
    public int MaxVotesPerColumn { get; }

    /// <inheritdoc />
    public bool IsSatisfiedBy(VoteEligibilityContext candidate) =>
        candidate.UserVoteCountInColumn < MaxVotesPerColumn;
}
