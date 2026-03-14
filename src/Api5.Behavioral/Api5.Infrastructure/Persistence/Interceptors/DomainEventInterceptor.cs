using Api5.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api5.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that dispatches domain events after SaveChanges
/// completes successfully.
/// </summary>
/// <remarks>
/// DESIGN: Domain events are dispatched AFTER the aggregate state is
/// persisted to the database. This ensures event handlers see committed
/// state. The interceptor:
///   1. Runs in the <c>SavedChangesAsync</c> hook (after save succeeds).
///   2. Collects all pending domain events from change-tracked entities
///      implementing <see cref="IHasDomainEvents"/>.
///   3. Clears the events from the entities (to prevent re-dispatch).
///   4. Publishes each event via MediatR's <see cref="IPublisher"/>.
///
/// Event handlers (e.g., <c>NoteRemovedEventHandler</c>) execute within
/// the same scope. If a handler needs to persist additional changes, it
/// calls <see cref="Api5.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/>
/// which triggers another round of SaveChanges (and potentially more events).
///
/// This is NEW in API 5. In API 1–4, side effects were handled inline
/// in service methods or relied on DB cascade deletes. Domain events
/// decouple the primary operation from its side effects.
/// </remarks>
public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    /// <summary>
    /// Initializes a new instance of <see cref="DomainEventInterceptor"/>.
    /// </summary>
    /// <param name="publisher">The MediatR publisher for dispatching events.</param>
    public DomainEventInterceptor(IPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <summary>
    /// After SaveChanges succeeds, collects and dispatches all pending
    /// domain events from tracked entities.
    /// </summary>
    /// <param name="eventData">The event data containing the DbContext.</param>
    /// <param name="result">The number of rows affected.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of rows affected.</returns>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            await DispatchDomainEventsAsync(eventData.Context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Collects domain events from all tracked entities and publishes them.
    /// </summary>
    /// <param name="context">The DbContext with tracked entities.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    private async Task DispatchDomainEventsAsync(
        Microsoft.EntityFrameworkCore.DbContext context,
        CancellationToken cancellationToken)
    {
        // Find all entities that have pending domain events
        List<IHasDomainEvents> entities = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        // Collect all events before clearing — we need a snapshot because
        // event handlers may modify entity state and trigger more saves.
        List<IDomainEvent> domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events from entities to prevent re-dispatch
        entities.ForEach(e => e.ClearDomainEvents());

        // Dispatch each event through MediatR
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }
    }
}
