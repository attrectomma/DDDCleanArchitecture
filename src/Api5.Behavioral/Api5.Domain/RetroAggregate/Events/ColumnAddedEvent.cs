using Api5.Domain.Common;

namespace Api5.Domain.RetroAggregate.Events;

/// <summary>
/// Raised when a new column is added to a retro board.
/// </summary>
/// <remarks>
/// DESIGN: This event signals that the RetroBoard aggregate's structure
/// changed. Handlers could update caches, send notifications, or log
/// the structural change for audit purposes.
/// </remarks>
/// <param name="RetroBoardId">The ID of the retro board the column was added to.</param>
/// <param name="ColumnId">The ID of the newly created column.</param>
/// <param name="ColumnName">The name of the new column.</param>
public record ColumnAddedEvent(Guid RetroBoardId, Guid ColumnId, string ColumnName) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
