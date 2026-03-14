namespace Api5.Application.DTOs.Responses;

/// <summary>Response DTO representing a project.</summary>
/// <param name="Id">The unique identifier of the project.</param>
/// <param name="Name">The name of the project.</param>
/// <param name="CreatedAt">The UTC timestamp when the project was created.</param>
public record ProjectResponse(Guid Id, string Name, DateTime CreatedAt);
