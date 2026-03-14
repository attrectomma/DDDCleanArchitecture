namespace Api2.Application.DTOs.Requests;

/// <summary>Request DTO for creating a new project.</summary>
/// <param name="Name">The name of the project.</param>
public record CreateProjectRequest(string Name);
