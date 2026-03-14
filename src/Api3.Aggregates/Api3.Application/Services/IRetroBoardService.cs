using Api3.Application.DTOs.Requests;
using Api3.Application.DTOs.Responses;

namespace Api3.Application.Services;

/// <summary>
/// Service contract for all retro board operations including columns,
/// notes, and votes.
/// </summary>
/// <remarks>
/// DESIGN: In API 2 there were separate services for columns, notes, and
/// votes (<c>IColumnService</c>, <c>INoteService</c>, <c>IVoteService</c>).
/// In API 3, a single <see cref="IRetroBoardService"/> covers the entire
/// RetroBoard aggregate because the aggregate root is the single entry
/// point for all mutations within its boundary.
///
/// DESIGN (CQRS foreshadowing): Notice that both reads and writes go
/// through this service. Reads load the full aggregate via the repository
/// even though they only need a projection. API 5 separates these into
/// distinct query handlers that bypass the aggregate entirely.
/// </remarks>
public interface IRetroBoardService
{
    /// <summary>Creates a new retro board within a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="request">The retro board creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created retro board response.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the project is not found.</exception>
    Task<RetroBoardResponse> CreateAsync(Guid projectId, CreateRetroBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a retro board by its ID, including all details.</summary>
    /// <param name="id">The retro board ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board response with details.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the retro board is not found.</exception>
    Task<RetroBoardResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a column to a retro board.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="request">The column creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created column response.</returns>
    Task<ColumnResponse> AddColumnAsync(Guid retroBoardId, CreateColumnRequest request, CancellationToken cancellationToken = default);

    /// <summary>Renames a column in a retro board.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="request">The column update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated column response.</returns>
    Task<ColumnResponse> RenameColumnAsync(Guid retroBoardId, Guid columnId, UpdateColumnRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a column from a retro board.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RemoveColumnAsync(Guid retroBoardId, Guid columnId, CancellationToken cancellationToken = default);

    /// <summary>Adds a note to a column.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="request">The note creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created note response.</returns>
    Task<NoteResponse> AddNoteAsync(Guid retroBoardId, Guid columnId, CreateNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a note's text.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="request">The note update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated note response.</returns>
    Task<NoteResponse> UpdateNoteAsync(Guid retroBoardId, Guid columnId, Guid noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a note from a column.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RemoveNoteAsync(Guid retroBoardId, Guid columnId, Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>Casts a vote on a note.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="request">The vote request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created vote response.</returns>
    Task<VoteResponse> CastVoteAsync(Guid retroBoardId, Guid columnId, Guid noteId, CastVoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a vote.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="voteId">The vote ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RemoveVoteAsync(Guid retroBoardId, Guid columnId, Guid noteId, Guid voteId, CancellationToken cancellationToken = default);

    // ── Convenience methods for external REST contract ──────────
    // These methods look up the aggregate by child entity ID,
    // bridging the gap between the URL structure (which uses
    // column/note IDs) and the aggregate-centric domain model.

    /// <summary>Adds a note to a column, looking up the retro board by column ID.</summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="request">The note creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created note response.</returns>
    Task<NoteResponse> AddNoteByColumnIdAsync(Guid columnId, CreateNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a note's text, looking up the retro board by column ID.</summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="request">The note update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated note response.</returns>
    Task<NoteResponse> UpdateNoteByColumnIdAsync(Guid columnId, Guid noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a note, looking up the retro board by column ID.</summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RemoveNoteByColumnIdAsync(Guid columnId, Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>Casts a vote on a note, looking up the retro board by note ID.</summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="request">The vote request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created vote response.</returns>
    Task<VoteResponse> CastVoteByNoteIdAsync(Guid noteId, CastVoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a vote, looking up the retro board by note ID.</summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="voteId">The vote ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RemoveVoteByNoteIdAsync(Guid noteId, Guid voteId, CancellationToken cancellationToken = default);
}
