using Api5.Application.DTOs.Responses;
using MediatR;

namespace Api5.Application.Retros.Commands.UpdateNote;

/// <summary>
/// Command to update a note's text (looked up by column ID + note ID).
/// </summary>
/// <param name="ColumnId">The ID of the column containing the note.</param>
/// <param name="NoteId">The ID of the note to update.</param>
/// <param name="NewText">The new text for the note.</param>
public record UpdateNoteCommand(Guid ColumnId, Guid NoteId, string NewText) : IRequest<NoteResponse>;
