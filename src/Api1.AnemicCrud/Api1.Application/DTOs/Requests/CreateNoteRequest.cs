namespace Api1.Application.DTOs.Requests;

/// <summary>Request DTO for creating a new note in a column.</summary>
/// <param name="Text">The text content of the note.</param>
public record CreateNoteRequest(string Text);
