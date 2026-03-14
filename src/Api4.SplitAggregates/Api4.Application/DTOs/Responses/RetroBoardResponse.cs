namespace Api4.Application.DTOs.Responses;

/// <summary>Response DTO representing a retro board with its columns.</summary>
/// <param name="Id">The unique identifier of the retro board.</param>
/// <param name="Name">The name of the retro board.</param>
/// <param name="ProjectId">The ID of the project this retro board belongs to.</param>
/// <param name="CreatedAt">The UTC timestamp when the retro board was created.</param>
/// <param name="Columns">The columns in this retro board, or <c>null</c> if not loaded.</param>
public record RetroBoardResponse(
    Guid Id,
    string Name,
    Guid ProjectId,
    DateTime CreatedAt,
    List<ColumnResponse>? Columns);
