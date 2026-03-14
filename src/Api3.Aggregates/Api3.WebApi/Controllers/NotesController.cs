using Api3.Application.DTOs.Requests;
using Api3.Application.DTOs.Responses;
using Api3.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api3.WebApi.Controllers;

/// <summary>
/// Controller for note operations within a column.
/// </summary>
/// <remarks>
/// DESIGN: In API 3, notes are part of the RetroBoard aggregate. However,
/// the external REST contract uses <c>/api/columns/{columnId}/notes</c> which
/// does not include the retro board ID. The service internally looks up the
/// aggregate by column ID using <see cref="IRetroBoardService.AddNoteByColumnIdAsync"/>.
///
/// This is a trade-off of maintaining backward-compatible URLs while using
/// aggregate-centric internal design. The aggregate is still the consistency
/// boundary; the controller simply bridges the URL structure.
/// </remarks>
[ApiController]
[Route("api/columns/{columnId:guid}/notes")]
public class NotesController : ControllerBase
{
    private readonly IRetroBoardService _retroBoardService;

    /// <summary>
    /// Initializes a new instance of <see cref="NotesController"/>.
    /// </summary>
    /// <param name="retroBoardService">The retro board service.</param>
    public NotesController(IRetroBoardService retroBoardService)
    {
        _retroBoardService = retroBoardService;
    }

    /// <summary>Creates a new note in a column.</summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="request">The note creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created note.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid columnId, CreateNoteRequest request, CancellationToken cancellationToken)
    {
        NoteResponse response = await _retroBoardService.AddNoteByColumnIdAsync(columnId, request, cancellationToken);
        return CreatedAtAction(nameof(Create), new { columnId, noteId = response.Id }, response);
    }

    /// <summary>Updates an existing note's text.</summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="request">The note update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated note.</returns>
    [HttpPut("{noteId:guid}")]
    [ProducesResponseType(typeof(NoteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid columnId, Guid noteId, UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        NoteResponse response = await _retroBoardService.UpdateNoteByColumnIdAsync(columnId, noteId, request, cancellationToken);
        return Ok(response);
    }

    /// <summary>Soft-deletes a note.</summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{noteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid columnId, Guid noteId, CancellationToken cancellationToken)
    {
        await _retroBoardService.RemoveNoteByColumnIdAsync(columnId, noteId, cancellationToken);
        return NoContent();
    }
}
