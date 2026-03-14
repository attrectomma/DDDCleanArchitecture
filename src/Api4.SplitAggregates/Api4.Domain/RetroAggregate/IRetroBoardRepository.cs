namespace Api4.Domain.RetroAggregate;

/// <summary>
/// Repository contract for the <see cref="RetroBoard"/> aggregate root.
/// Loads/saves the aggregate (board + columns + notes) WITHOUT votes.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 3's repository that loaded the full graph including
/// votes. In API 4, votes are a separate aggregate with their own repository
/// (<see cref="Api4.Domain.VoteAggregate.IVoteRepository"/>). The RetroBoard
/// repository no longer eager-loads votes.
///
/// A new <see cref="NoteExistsAsync"/> method is added for lightweight
/// cross-aggregate validation — used by VoteService to check note existence
/// before creating a vote without loading the entire aggregate.
///
/// DESIGN (CQRS foreshadowing): This same eager-loading query still runs for
/// BOTH writes AND reads. API 5 introduces CQRS to separate the read path
/// (lightweight no-tracking projections) from the write path.
/// </remarks>
public interface IRetroBoardRepository
{
    /// <summary>
    /// Retrieves a retro board by ID with all columns and notes loaded (no votes).
    /// </summary>
    /// <param name="id">The retro board's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board aggregate, or <c>null</c> if not found.</returns>
    Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new retro board to the repository.</summary>
    /// <param name="board">The retro board to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(RetroBoard board, CancellationToken cancellationToken = default);

    /// <summary>Marks a retro board for deletion.</summary>
    /// <param name="board">The retro board to delete.</param>
    void Delete(RetroBoard board);

    /// <summary>
    /// Retrieves the retro board that contains the specified column.
    /// Loads the aggregate graph (columns + notes, no votes).
    /// </summary>
    /// <param name="columnId">The column's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board aggregate, or <c>null</c> if not found.</returns>
    Task<RetroBoard?> GetByColumnIdAsync(Guid columnId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the retro board that contains the specified note.
    /// Loads the aggregate graph (columns + notes, no votes).
    /// </summary>
    /// <param name="noteId">The note's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board aggregate, or <c>null</c> if not found.</returns>
    Task<RetroBoard?> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lightweight check: does a note with this ID exist?
    /// Used by VoteService for cross-aggregate validation.
    /// </summary>
    /// <param name="noteId">The note's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the note exists (and is not soft-deleted); otherwise <c>false</c>.</returns>
    /// <remarks>
    /// DESIGN: This method is NEW in API 4. It exists because the VoteService
    /// needs to verify that a note exists before creating a vote. Loading the
    /// full RetroBoard aggregate just to check note existence would be wasteful.
    /// This lightweight query avoids the overhead.
    /// </remarks>
    Task<bool> NoteExistsAsync(Guid noteId, CancellationToken cancellationToken = default);
}
