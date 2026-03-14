using Api5.Application.DTOs.Requests;
using Api5.Application.DTOs.Responses;
using Api5.Application.Retros.Commands.AddNote;
using Api5.Application.Retros.Commands.RemoveNote;
using Api5.Application.Retros.Commands.UpdateNote;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api5.WebApi.Controllers;

/// <summary>
/// Controller for note operations within a column.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 4's NotesController that depended on
/// <c>IRetroBoardService</c>. Here, each action creates the appropriate
/// command and sends it through MediatR. The controller has no knowledge
/// of aggregates, repositories, or business logic.
///
/// Note removal triggers a domain event (<c>NoteRemovedEvent</c>) that is
/// dispatched by the <c>DomainEventInterceptor</c> after save. The
/// <c>NoteRemovedEventHandler</c> then cleans up orphaned votes — all
/// without the controller being aware of this side effect.
/// </remarks>
[ApiController]
[Route("api/columns/{columnId:guid}/notes")]
public class NotesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of <see cref="NotesController"/>.
    /// </summary>
    /// <param name="mediator">The MediatR mediator.</param>
    public NotesController(IMediator mediator)
    {
        _mediator = mediator;
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
        var command = new AddNoteCommand(columnId, request.Text);
        NoteResponse response = await _mediator.Send(command, cancellationToken);
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
        var command = new UpdateNoteCommand(columnId, noteId, request.Text);
        NoteResponse response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    /// <summary>Soft-deletes a note.</summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="noteId">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on success.</returns>
    /// <remarks>
    /// DESIGN: When this endpoint is called, the <c>RemoveNoteCommandHandler</c>
    /// removes the note from the aggregate, which raises a <c>NoteRemovedEvent</c>.
    /// After <c>SaveChangesAsync</c>, the <c>DomainEventInterceptor</c> dispatches
    /// the event to <c>NoteRemovedEventHandler</c>, which cascades the soft-delete
    /// to all associated votes. The controller is completely unaware of this chain.
    /// </remarks>
    [HttpDelete("{noteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid columnId, Guid noteId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveNoteCommand(columnId, noteId), cancellationToken);
        return NoContent();
    }
}
