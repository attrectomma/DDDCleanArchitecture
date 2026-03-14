namespace Api2.Application.DTOs.Requests;

/// <summary>Request DTO for adding a user as a member to a project.</summary>
/// <param name="UserId">The ID of the user to add.</param>
public record AddMemberRequest(Guid UserId);
