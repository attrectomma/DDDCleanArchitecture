namespace Api5.Application.DTOs.Requests;

/// <summary>Request DTO for creating a new retro board within a project.</summary>
/// <param name="Name">The name of the retro board.</param>
public record CreateRetroBoardRequest(string Name);
