using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;

namespace Api4.Application.Services;

/// <summary>
/// Service contract for retro board operations including columns and notes,
/// but NOT votes. Votes are handled by <see cref="IVoteService"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 3, a single <see cref="IRetroBoardService"/> covered columns,
/// notes, AND votes because they were all inside the RetroBoard aggregate.
/// In API 4, Vote is its own aggregate, so vote operations move to
/// <see cref="IVoteService"/>. This service covers retro board, column,
/// and note operations only.
///
/// DESIGN (CQRS foreshadowing): Both reads and writes go through this service.
/// Reads load the full aggregate via the repository even though they only need
/// a projection. API 5 separates these with CQRS.
/// </remarks>
public interface IRetroBoardService
{
    /// <summary>Creates a new retro board within a project.</summary>
    Task<RetroBoardResponse> CreateAsync(Guid projectId, CreateRetroBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a retro board by its ID, including all details.</summary>
    Task<RetroBoardResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a column to a retro board.</summary>
    Task<ColumnResponse> AddColumnAsync(Guid retroBoardId, CreateColumnRequest request, CancellationToken cancellationToken = default);

    /// <summary>Renames a column in a retro board.</summary>
    Task<ColumnResponse> RenameColumnAsync(Guid retroBoardId, Guid columnId, UpdateColumnRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a column from a retro board.</summary>
    Task RemoveColumnAsync(Guid retroBoardId, Guid columnId, CancellationToken cancellationToken = default);

    /// <summary>Adds a note to a column, looking up the retro board by column ID.</summary>
    Task<NoteResponse> AddNoteByColumnIdAsync(Guid columnId, CreateNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates a note's text, looking up the retro board by column ID.</summary>
    Task<NoteResponse> UpdateNoteByColumnIdAsync(Guid columnId, Guid noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a note, looking up the retro board by column ID.</summary>
    Task RemoveNoteByColumnIdAsync(Guid columnId, Guid noteId, CancellationToken cancellationToken = default);

    // ❌ NO vote operations — those are on IVoteService
}
