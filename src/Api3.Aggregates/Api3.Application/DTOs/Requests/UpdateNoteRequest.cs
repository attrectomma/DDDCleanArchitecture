namespace Api3.Application.DTOs.Requests;

/// <summary>Request DTO for updating an existing note's text.</summary>
/// <param name="Text">The new text for the note.</param>
public record UpdateNoteRequest(string Text);
