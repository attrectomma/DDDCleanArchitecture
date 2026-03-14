namespace Api5.Domain.RetroAggregate;

/// <summary>
/// Repository contract for the <see cref="RetroBoard"/> aggregate root.
/// Loads/saves the aggregate (board + columns + notes) WITHOUT votes.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 4's repository. In API 5, this repository is used
/// ONLY by command handlers (write side). Query handlers bypass the
/// repository entirely and project directly from <c>DbContext</c> — this
/// is the core CQRS insight.
///
/// Compare with API 4 where the same <c>RetroBoardService.GetByIdAsync</c>
/// method used this repository for BOTH reads and writes. That meant every
/// GET request loaded the full aggregate graph with change tracking, only
/// to map it to a DTO and discard it. API 5's query handlers avoid this
/// waste entirely.
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
    /// Used by command handlers for cross-aggregate validation.
    /// </summary>
    /// <param name="noteId">The note's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the note exists (and is not soft-deleted); otherwise <c>false</c>.</returns>
    /// <remarks>
    /// DESIGN: Same as API 4. This lightweight query avoids loading the full
    /// RetroBoard aggregate just to check note existence. Used by the
    /// <c>CastVoteCommandHandler</c> for cross-aggregate validation.
    /// </remarks>
    Task<bool> NoteExistsAsync(Guid noteId, CancellationToken cancellationToken = default);
}
