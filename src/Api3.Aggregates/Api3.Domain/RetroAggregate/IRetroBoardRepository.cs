namespace Api3.Domain.RetroAggregate;

/// <summary>
/// Repository contract for the <see cref="RetroBoard"/> aggregate root.
/// Loads/saves the ENTIRE aggregate (board + columns + notes + votes).
/// </summary>
/// <remarks>
/// DESIGN: There is no <c>IColumnRepository</c>, <c>INoteRepository</c>, or
/// <c>IVoteRepository</c>. All child entities are reached through the aggregate
/// root. The repository always loads the complete aggregate graph. This ensures
/// the aggregate root has full state to enforce all invariants.
///
/// DESIGN (CQRS foreshadowing): This same expensive eager-loading query runs
/// for BOTH writes (where the full state is needed for invariants) AND reads
/// (where we only need a DTO). API 5 introduces CQRS to separate the read
/// path (lightweight no-tracking projections) from the write path.
/// </remarks>
public interface IRetroBoardRepository
{
    /// <summary>
    /// Retrieves a retro board by ID with all columns, notes, and votes loaded.
    /// </summary>
    /// <param name="id">The retro board's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The complete retro board aggregate, or <c>null</c> if not found.</returns>
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
    /// Loads the full aggregate graph.
    /// </summary>
    /// <param name="columnId">The column's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The complete retro board aggregate, or <c>null</c> if not found.</returns>
    /// <remarks>
    /// DESIGN: This method exists to support the external REST contract where
    /// note and column operations use URLs like <c>/api/columns/{columnId}/notes</c>
    /// that do not include the retro board ID. Internally, the aggregate must
    /// be loaded by its root ID — this method bridges the gap by looking up
    /// the retro board from a child entity ID.
    /// </remarks>
    Task<RetroBoard?> GetByColumnIdAsync(Guid columnId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the retro board that contains the specified note.
    /// Loads the full aggregate graph.
    /// </summary>
    /// <param name="noteId">The note's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The complete retro board aggregate, or <c>null</c> if not found.</returns>
    Task<RetroBoard?> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default);
}
