using Api5.Domain.Common.Specifications;
using Api5.Domain.Exceptions;
using Api5.Domain.VoteAggregate.Specifications;

namespace Api5.Domain.VoteAggregate.Strategies;

/// <summary>
/// Default voting strategy: one vote per user per note. This replicates the
/// voting behaviour of API 1–4 using the Strategy and Specification patterns.
/// </summary>
/// <remarks>
/// DESIGN: The Default strategy composes three specifications using AND:
/// <list type="number">
///   <item><see cref="NoteExistsSpecification"/> — the note must exist.</item>
///   <item><see cref="UserIsProjectMemberSpecification"/> — the user must be a project member.</item>
///   <item><see cref="UniqueVotePerNoteSpecification"/> — the user must not have already voted on this note.</item>
/// </list>
///
/// Compare with <see cref="BudgetVotingStrategy"/> which OMITS the uniqueness
/// specification and ADDS a budget specification instead. The Specification
/// pattern makes this difference explicit: different strategies compose
/// different subsets of the same reusable specifications.
///
/// The <see cref="Validate"/> method checks each specification individually
/// to provide targeted error messages. The <see cref="Rules"/> property
/// exposes the composite for quick boolean checks without error detail.
/// </remarks>
public class DefaultVotingStrategy : IVotingStrategy
{
    private readonly NoteExistsSpecification _noteExists = new();
    private readonly UserIsProjectMemberSpecification _isMember = new();
    private readonly UniqueVotePerNoteSpecification _uniqueVote = new();

    /// <inheritdoc />
    /// <remarks>
    /// Composite rule: NoteExists AND UserIsProjectMember AND UniqueVotePerNote.
    ///
    /// DESIGN (dual-surface): This composite is not called in the current production
    /// path — <see cref="Validate"/> is used instead because it provides per-rule error
    /// messages. The composite exists to demonstrate specification composability and to
    /// support future scenarios like bulk eligibility checks or UI pre-flight queries
    /// (see <see cref="IVotingStrategy.Rules"/> for details).
    /// </remarks>
    public ISpecification<VoteEligibilityContext> Rules =>
        _noteExists
            .And(_isMember)
            .And(_uniqueVote);

    /// <inheritdoc />
    public void Validate(VoteEligibilityContext context)
    {
        if (!_noteExists.IsSatisfiedBy(context))
            throw new DomainException(
                $"Note {context.NoteId} does not exist.");

        if (!_isMember.IsSatisfiedBy(context))
            throw new InvariantViolationException(
                $"User {context.UserId} is not a member of project {context.ProjectId} and cannot vote.");

        if (!_uniqueVote.IsSatisfiedBy(context))
            throw new InvariantViolationException(
                $"User {context.UserId} has already voted on note {context.NoteId}.");
    }
}
