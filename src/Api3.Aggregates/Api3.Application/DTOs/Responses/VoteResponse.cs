namespace Api3.Application.DTOs.Responses;

/// <summary>Response DTO representing a vote.</summary>
/// <param name="Id">The unique identifier of the vote.</param>
/// <param name="NoteId">The ID of the note this vote is for.</param>
/// <param name="UserId">The ID of the user who cast this vote.</param>
public record VoteResponse(Guid Id, Guid NoteId, Guid UserId);
