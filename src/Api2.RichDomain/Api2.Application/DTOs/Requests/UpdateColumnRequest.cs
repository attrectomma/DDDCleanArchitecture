namespace Api2.Application.DTOs.Requests;

/// <summary>Request DTO for updating an existing column's name.</summary>
/// <param name="Name">The new name for the column.</param>
public record UpdateColumnRequest(string Name);
