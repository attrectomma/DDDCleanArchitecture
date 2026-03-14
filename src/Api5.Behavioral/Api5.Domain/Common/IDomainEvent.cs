using MediatR;

namespace Api5.Domain.Common;

/// <summary>
/// Marker interface for domain events — things that "happened"
/// within an aggregate that other parts of the system may care about.
/// </summary>
/// <remarks>
/// DESIGN: Domain events decouple side effects from the aggregate
/// that raises them. In API 4, when a note is deleted, the VoteService
/// had to explicitly handle vote cleanup (or rely on DB cascade delete).
/// With domain events, the aggregate raises a <c>NoteRemovedEvent</c> and
/// a handler in the Application layer reacts. This follows the Open/Closed
/// Principle — new reactions can be added without modifying the aggregate.
///
/// This interface extends MediatR's <see cref="INotification"/> so domain
/// events can be dispatched through the MediatR pipeline and handled by
/// <c>INotificationHandler&lt;T&gt;</c> implementations.
/// </remarks>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// Gets the UTC timestamp when this event was created.
    /// </summary>
    DateTime OccurredOn { get; }
}
