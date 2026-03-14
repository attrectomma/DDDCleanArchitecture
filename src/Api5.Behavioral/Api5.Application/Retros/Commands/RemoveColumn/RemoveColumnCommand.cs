using Api5.Application.Common.Interfaces;
using MediatR;

namespace Api5.Application.Retros.Commands.RemoveColumn;

/// <summary>
/// Command to remove a column from a retro board.
/// </summary>
/// <param name="RetroBoardId">The ID of the retro board.</param>
/// <param name="ColumnId">The ID of the column to remove.</param>
public record RemoveColumnCommand(Guid RetroBoardId, Guid ColumnId) : ICommand<Unit>;
