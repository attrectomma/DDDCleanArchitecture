namespace Api5.Domain.VoteAggregate;

/// <summary>
/// Repository contract for the <see cref="Vote"/> aggregate root.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 4. In API 5, this repository is used ONLY by
/// command handlers (write side): <c>CastVoteCommandHandler</c> and
/// <c>RemoveVoteCommandHandler</c>. Query handlers that need vote data
/// bypass the repository and query <c>DbContext</c> directly.
///
/// The <see cref="GetByNoteIdAsync"/> method is retained because it is
/// used by the <c>NoteRemovedEventHandler</c> to clean up orphaned votes
/// when a note is removed — a write-side operation triggered by a domain event.
/// </remarks>
public interface IVoteRepository
{
    /// <summary>Retrieves a vote by its unique identifier.</summary>
    /// <param name="id">The vote's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The vote, or <c>null</c> if not found.</returns>
    Task<Vote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a vote already exists for the given note and user combination.
    /// </summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a vote exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(Guid noteId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new vote to the repository.</summary>
    /// <param name="vote">The vote to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(Vote vote, CancellationToken cancellationToken = default);

    /// <summary>Marks a vote for deletion.</summary>
    /// <param name="vote">The vote to delete.</param>
    void Delete(Vote vote);

    /// <summary>
    /// Retrieves all votes for a given note.
    /// Used by the <c>NoteRemovedEventHandler</c> to clean up orphaned votes.
    /// </summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of votes for the note.</returns>
    Task<List<Vote>> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of votes a user has cast on notes within a specific column.
    /// Used to enforce the per-column vote budget in the
    /// <see cref="Strategies.BudgetVotingStrategy"/>.
    /// </summary>
    /// <param name="columnId">The column whose notes to count votes for.</param>
    /// <param name="userId">The user whose votes to count.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of votes the user has cast in the column.</returns>
    /// <remarks>
    /// DESIGN: This is a cross-aggregate query — Vote and Note belong to different
    /// aggregates. The repository implementation joins Vote with Note to resolve
    /// the column association. This method is only needed by the Budget voting
    /// strategy; the Default strategy does not call it.
    /// </remarks>
    Task<int> CountByColumnAndUserAsync(Guid columnId, Guid userId, CancellationToken cancellationToken = default);
}
