namespace Api5.Application.DTOs.Responses;

/// <summary>Response DTO representing a column with its notes.</summary>
/// <param name="Id">The unique identifier of the column.</param>
/// <param name="Name">The name of the column.</param>
/// <param name="Notes">The notes in this column, or <c>null</c> if not loaded.</param>
public record ColumnResponse(Guid Id, string Name, List<NoteResponse>? Notes);
