using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;
using Api1.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api1.WebApi.Controllers;

/// <summary>
/// Controller for note operations within a column.
/// </summary>
[ApiController]
[Route("api/columns/{columnId:guid}/notes")]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    /// <summary>
    /// Initializes a new instance of <see cref="NotesController"/>.
    /// </summary>
    /// <param name="noteService">The note service.</param>
    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
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
        NoteResponse response = await _noteService.CreateAsync(columnId, request, cancellationToken);
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
        NoteResponse response = await _noteService.UpdateAsync(columnId, noteId, request, cancellationToken);
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
        await _noteService.DeleteAsync(columnId, noteId, cancellationToken);
        return NoContent();
    }
}
