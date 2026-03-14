using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;

namespace Api5.Application.Retros.Commands.AddColumn;

/// <summary>
/// Command to add a column to a retro board.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a WRITE operation. We MUST load the aggregate
/// through the repository so the aggregate root can enforce invariants
/// (e.g., unique column names). The aggregate is the write model.
/// </remarks>
/// <param name="RetroBoardId">The ID of the retro board.</param>
/// <param name="Name">The name of the new column.</param>
public record AddColumnCommand(Guid RetroBoardId, string Name) : ICommand<ColumnResponse>;
