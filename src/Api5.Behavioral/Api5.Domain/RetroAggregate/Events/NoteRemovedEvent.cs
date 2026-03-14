using Api5.Domain.Common;

namespace Api5.Domain.RetroAggregate.Events;

/// <summary>
/// Raised when a note is removed from a column within a retro board.
/// </summary>
/// <remarks>
/// DESIGN: This is the most important domain event in API 5. When a note
/// is removed, any associated votes (in the separate Vote aggregate) must
/// be cleaned up. In API 4, this cleanup was handled either by:
///   - DB cascade delete (FK from Vote to Note with ON DELETE CASCADE), or
///   - Explicit cleanup in the VoteService.
///
/// With domain events, the RetroBoard aggregate raises this event and the
/// <c>NoteRemovedEventHandler</c> in the Application layer reacts by
/// soft-deleting orphaned votes. The RetroBoard aggregate remains unaware
/// of the Vote aggregate entirely — true separation of concerns.
/// </remarks>
/// <param name="NoteId">The ID of the removed note.</param>
/// <param name="ColumnId">The ID of the column the note was removed from.</param>
public record NoteRemovedEvent(Guid NoteId, Guid ColumnId) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
