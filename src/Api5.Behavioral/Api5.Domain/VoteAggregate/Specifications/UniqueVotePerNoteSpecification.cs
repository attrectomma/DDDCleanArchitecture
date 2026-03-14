using Api5.Domain.Common.Specifications;
using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Domain.VoteAggregate.Specifications;

/// <summary>
/// Specification that checks whether the user has NOT already voted on the
/// target note. Used by the <see cref="DefaultVotingStrategy"/> to enforce
/// the one-vote-per-user-per-note invariant.
/// </summary>
/// <remarks>
/// DESIGN: In API 1–4, the "already voted" check was inline:
/// <code>
///   if (await _voteRepository.ExistsAsync(noteId, userId, ct))
///       throw new InvariantViolationException(...);
/// </code>
///
/// In API 5 with the Specification pattern, this rule is a standalone,
/// testable object. The <see cref="DefaultVotingStrategy"/> composes it
/// with other specifications via AND. The <see cref="BudgetVotingStrategy"/>
/// intentionally OMITS this specification because it allows multiple votes
/// on the same note.
///
/// This difference in composition is the key educational point of combining
/// the Strategy and Specification patterns: the same reusable specification
/// is included or excluded depending on the strategy.
/// </remarks>
public class UniqueVotePerNoteSpecification : ISpecification<VoteEligibilityContext>
{
    /// <inheritdoc />
    public bool IsSatisfiedBy(VoteEligibilityContext candidate) =>
        !candidate.UserAlreadyVotedOnNote;
}
