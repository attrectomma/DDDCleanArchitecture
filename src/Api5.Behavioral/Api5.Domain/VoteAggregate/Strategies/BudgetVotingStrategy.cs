using Api5.Domain.Common.Specifications;
using Api5.Domain.Exceptions;
using Api5.Domain.VoteAggregate.Specifications;

namespace Api5.Domain.VoteAggregate.Strategies;

/// <summary>
/// Budget voting strategy: each user gets a fixed number of votes per column
/// and may place multiple votes on the same note ("dot voting").
/// </summary>
/// <remarks>
/// DESIGN: The Budget strategy composes three specifications using AND:
/// <list type="number">
///   <item><see cref="NoteExistsSpecification"/> — the note must exist.</item>
///   <item><see cref="UserIsProjectMemberSpecification"/> — the user must be a project member.</item>
///   <item><see cref="VoteBudgetNotExceededSpecification"/> — the user must have remaining votes in the column.</item>
/// </list>
///
/// Crucially, this strategy does NOT include <see cref="UniqueVotePerNoteSpecification"/>.
/// Users CAN vote multiple times on the same note, as long as they have budget remaining
/// in the column. This enables "dot voting" — a common retrospective technique where
/// participants signal the strength of their agreement by placing multiple dots (votes)
/// on the items they feel most strongly about.
///
/// The difference in specification composition between this strategy and
/// <see cref="DefaultVotingStrategy"/> is the key educational point: the Strategy
/// pattern selects WHICH specifications to compose, and the Specification pattern
/// makes each rule reusable and testable in isolation.
///
/// DESIGN: Because this strategy allows multiple votes on the same note, the
/// database unique constraint on <c>(NoteId, UserId)</c> has been replaced with
/// a non-unique index. The <see cref="DefaultVotingStrategy"/> now enforces
/// uniqueness at the application level via <see cref="UniqueVotePerNoteSpecification"/>.
/// This trade-off is documented in the VoteConfiguration and the pattern documentation.
/// </remarks>
public class BudgetVotingStrategy : IVotingStrategy
{
    /// <summary>
    /// The default maximum number of votes a user may cast per column.
    /// </summary>
    public const int DefaultMaxVotesPerColumn = 3;

    private readonly NoteExistsSpecification _noteExists = new();
    private readonly UserIsProjectMemberSpecification _isMember = new();
    private readonly VoteBudgetNotExceededSpecification _budget;

    /// <summary>
    /// Initializes a new instance of <see cref="BudgetVotingStrategy"/>
    /// with the default vote budget per column.
    /// </summary>
    public BudgetVotingStrategy()
        : this(DefaultMaxVotesPerColumn)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="BudgetVotingStrategy"/>
    /// with a custom vote budget per column.
    /// </summary>
    /// <param name="maxVotesPerColumn">The maximum number of votes per user per column.</param>
    public BudgetVotingStrategy(int maxVotesPerColumn)
    {
        _budget = new VoteBudgetNotExceededSpecification(maxVotesPerColumn);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Composite rule: NoteExists AND UserIsProjectMember AND VoteBudgetNotExceeded.
    /// Note: UniqueVotePerNote is intentionally absent — multiple votes on the
    /// same note are allowed under budget voting.
    /// </remarks>
    public ISpecification<VoteEligibilityContext> Rules =>
        _noteExists
            .And(_isMember)
            .And(_budget);

    /// <inheritdoc />
    public void Validate(VoteEligibilityContext context)
    {
        if (!_noteExists.IsSatisfiedBy(context))
            throw new DomainException(
                $"Note {context.NoteId} does not exist.");

        if (!_isMember.IsSatisfiedBy(context))
            throw new InvariantViolationException(
                $"User {context.UserId} is not a member of project {context.ProjectId} and cannot vote.");

        if (!_budget.IsSatisfiedBy(context))
            throw new InvariantViolationException(
                $"User {context.UserId} has reached the maximum of {_budget.MaxVotesPerColumn} votes in this column.");
    }
}
