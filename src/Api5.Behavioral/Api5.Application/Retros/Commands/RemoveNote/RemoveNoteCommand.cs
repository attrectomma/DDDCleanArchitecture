using MediatR;

namespace Api5.Application.Retros.Commands.RemoveNote;

/// <summary>
/// Command to remove a note from a column (looked up by column ID + note ID).
/// </summary>
/// <remarks>
/// DESIGN: When the handler removes the note from the aggregate, the
/// RetroBoard raises a <c>NoteRemovedEvent</c>. After save, the
/// <c>DomainEventInterceptor</c> dispatches this event, and the
/// <c>NoteRemovedEventHandler</c> cleans up orphaned votes.
/// </remarks>
/// <param name="ColumnId">The ID of the column containing the note.</param>
/// <param name="NoteId">The ID of the note to remove.</param>
public record RemoveNoteCommand(Guid ColumnId, Guid NoteId) : IRequest<Unit>;
