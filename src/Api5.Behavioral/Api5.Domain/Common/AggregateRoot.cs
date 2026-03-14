namespace Api5.Domain.Common;

/// <summary>
/// Enhanced aggregate root base that supports domain event collection.
/// All aggregate roots in API 5 inherit from this class instead of
/// directly from <see cref="AuditableEntityBase"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 3/4, aggregate roots inherited directly from
/// <see cref="AuditableEntityBase"/> and implemented <see cref="IAggregateRoot"/>
/// as a marker. In API 5, this base class adds domain event support.
/// Aggregate roots call <see cref="RaiseDomainEvent"/> to record events,
/// and the infrastructure layer's <c>DomainEventInterceptor</c> dispatches
/// them after <c>SaveChangesAsync</c> succeeds.
///
/// This pattern keeps the domain model expressive: instead of returning
/// flags or relying on callers to trigger side effects, aggregates
/// declare that something happened via events. The event handlers decide
/// what to do about it.
/// </remarks>
public abstract class AggregateRoot : AuditableEntityBase, IAggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Registers a domain event to be dispatched after save.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
