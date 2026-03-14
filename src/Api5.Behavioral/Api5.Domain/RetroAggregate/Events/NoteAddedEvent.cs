using Api5.Domain.Common;

namespace Api5.Domain.RetroAggregate.Events;

/// <summary>
/// Raised when a new note is added to a column within a retro board.
/// </summary>
/// <remarks>
/// DESIGN: This event signals that content was added to the board.
/// Handlers could notify other team members or trigger real-time updates.
/// </remarks>
/// <param name="RetroBoardId">The ID of the retro board containing the column.</param>
/// <param name="ColumnId">The ID of the column the note was added to.</param>
/// <param name="NoteId">The ID of the newly created note.</param>
public record NoteAddedEvent(Guid RetroBoardId, Guid ColumnId, Guid NoteId) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
