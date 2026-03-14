using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;

namespace Api5.Application.Retros.Commands.RenameColumn;

/// <summary>
/// Command to rename a column in a retro board.
/// </summary>
/// <param name="RetroBoardId">The ID of the retro board.</param>
/// <param name="ColumnId">The ID of the column to rename.</param>
/// <param name="NewName">The new name for the column.</param>
public record RenameColumnCommand(Guid RetroBoardId, Guid ColumnId, string NewName) : ICommand<ColumnResponse>;
