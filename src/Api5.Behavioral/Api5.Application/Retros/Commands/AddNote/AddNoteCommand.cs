using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;

namespace Api5.Application.Retros.Commands.AddNote;

/// <summary>
/// Command to add a note to a column (looked up by column ID).
/// </summary>
/// <param name="ColumnId">The ID of the column to add the note to.</param>
/// <param name="Text">The text content of the note.</param>
public record AddNoteCommand(Guid ColumnId, string Text) : ICommand<NoteResponse>;
