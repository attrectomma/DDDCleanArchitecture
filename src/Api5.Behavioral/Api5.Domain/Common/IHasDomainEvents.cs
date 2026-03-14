namespace Api5.Domain.Common;

/// <summary>
/// Interface for entities that can raise domain events.
/// Typically implemented by aggregate roots.
/// </summary>
/// <remarks>
/// DESIGN: This interface allows the infrastructure layer
/// (specifically the <c>DomainEventInterceptor</c>) to discover
/// entities with pending domain events after <c>SaveChangesAsync</c>
/// completes and dispatch them via MediatR. The interceptor iterates
/// over change-tracked entities implementing this interface, collects
/// their events, clears the events from the entity, and publishes
/// them. This ensures events are dispatched only after the aggregate
/// state is successfully persisted.
/// </remarks>
public interface IHasDomainEvents
{
    /// <summary>
    /// Gets the collection of domain events that have been raised
    /// by this entity but not yet dispatched.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>
    /// Clears all pending domain events after they have been dispatched.
    /// Called by the infrastructure layer after publishing.
    /// </summary>
    void ClearDomainEvents();
}
