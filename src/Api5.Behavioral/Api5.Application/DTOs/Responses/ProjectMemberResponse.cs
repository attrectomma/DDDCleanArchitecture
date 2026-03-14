namespace Api5.Application.DTOs.Responses;

/// <summary>Response DTO representing a project member assignment.</summary>
/// <param name="Id">The unique identifier of the membership.</param>
/// <param name="ProjectId">The ID of the project.</param>
/// <param name="UserId">The ID of the user.</param>
public record ProjectMemberResponse(Guid Id, Guid ProjectId, Guid UserId);
