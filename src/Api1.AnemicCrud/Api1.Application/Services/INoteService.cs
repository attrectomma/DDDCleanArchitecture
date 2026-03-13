using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;

namespace Api1.Application.Services;

/// <summary>
/// Service contract for note-related operations.
/// </summary>
public interface INoteService
{
    /// <summary>Creates a new note in a column.</summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="request">The note creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created note response.</returns>
    Task<NoteResponse> CreateAsync(Guid columnId, CreateNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing note's text.</summary>
    /// <param name="columnId">The column ID (for route context).</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="request">The note update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated note response.</returns>
    Task<NoteResponse> UpdateAsync(Guid columnId, Guid noteId, UpdateNoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a note.</summary>
    /// <param name="columnId">The column ID (for route context).</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DeleteAsync(Guid columnId, Guid noteId, CancellationToken cancellationToken = default);
}
