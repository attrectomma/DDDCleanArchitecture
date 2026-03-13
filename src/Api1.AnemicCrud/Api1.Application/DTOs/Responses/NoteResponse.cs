namespace Api1.Application.DTOs.Responses;

/// <summary>Response DTO representing a note with its vote count.</summary>
/// <param name="Id">The unique identifier of the note.</param>
/// <param name="Text">The text content of the note.</param>
/// <param name="VoteCount">The number of votes on this note, or <c>null</c> if not computed.</param>
public record NoteResponse(Guid Id, string Text, int? VoteCount);
