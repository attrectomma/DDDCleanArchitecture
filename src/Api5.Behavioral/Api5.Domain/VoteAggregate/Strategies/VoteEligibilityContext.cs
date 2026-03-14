namespace Api5.Domain.VoteAggregate.Strategies;

/// <summary>
/// Immutable context object containing all data that voting specifications
/// need to evaluate vote eligibility. Built by the command handler from
/// repository queries before being passed to the <see cref="IVotingStrategy"/>.
/// </summary>
/// <remarks>
/// DESIGN: This context separates data gathering (infrastructure concern,
/// handled by the command handler) from rule evaluation (domain concern,
/// handled by specifications). Specifications receive this pre-built context
/// and evaluate pure boolean conditions — no async calls, no repository
/// dependencies, fully unit-testable.
///
/// Compare with API 4's <c>CastVoteAsync</c> where data fetching and rule
/// checking were interleaved. Here, the handler builds the context once,
/// and the strategy evaluates it synchronously.
/// </remarks>
/// <param name="NoteId">The ID of the note being voted on.</param>
/// <param name="UserId">The ID of the user casting the vote.</param>
/// <param name="ColumnId">The ID of the column containing the note.</param>
/// <param name="RetroBoardId">The ID of the retro board.</param>
/// <param name="ProjectId">The ID of the project the retro board belongs to.</param>
/// <param name="NoteExists">Whether the target note exists (not soft-deleted).</param>
/// <param name="UserIsProjectMember">
/// Whether the user is a member of the project. <c>true</c> if the project has
/// no members (open access) or the user is explicitly listed.
/// </param>
/// <param name="UserAlreadyVotedOnNote">Whether the user has already cast a vote on this note.</param>
/// <param name="UserVoteCountInColumn">
/// The number of votes the user has already cast on notes in this column.
/// Used by the <see cref="BudgetVotingStrategy"/> to enforce the per-column budget.
/// </param>
public record VoteEligibilityContext(
    Guid NoteId,
    Guid UserId,
    Guid ColumnId,
    Guid RetroBoardId,
    Guid ProjectId,
    bool NoteExists,
    bool UserIsProjectMember,
    bool UserAlreadyVotedOnNote,
    int UserVoteCountInColumn);
