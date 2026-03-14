namespace Api5.Application.DTOs.Requests;

/// <summary>Request DTO for creating a new column in a retro board.</summary>
/// <param name="Name">The name of the column.</param>
public record CreateColumnRequest(string Name);
