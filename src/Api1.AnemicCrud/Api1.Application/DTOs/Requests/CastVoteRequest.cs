namespace Api1.Application.DTOs.Requests;

/// <summary>Request DTO for casting a vote on a note.</summary>
/// <param name="UserId">The ID of the user casting the vote.</param>
public record CastVoteRequest(Guid UserId);
