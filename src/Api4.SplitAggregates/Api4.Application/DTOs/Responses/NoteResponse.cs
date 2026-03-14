namespace Api4.Application.DTOs.Responses;

/// <summary>Response DTO representing a note with its vote count.</summary>
/// <param name="Id">The unique identifier of the note.</param>
/// <param name="Text">The text content of the note.</param>
/// <param name="VoteCount">The number of votes on this note, or <c>null</c> if not computed.</param>
/// <remarks>
/// DESIGN: In API 3, VoteCount came from <c>note.Votes.Count</c> because votes
/// were part of the RetroBoard aggregate. In API 4, Vote is a separate aggregate,
/// so vote counts must be queried separately via the Vote repository. This is
/// an example of the read-side cost of splitting aggregates.
/// </remarks>
public record NoteResponse(Guid Id, string Text, int? VoteCount);
