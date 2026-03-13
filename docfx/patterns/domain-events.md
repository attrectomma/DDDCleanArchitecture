# Domain Events

## Intent

Decouple **side effects** from the primary operation that caused them. When
something significant happens in the domain, the aggregate raises an event,
and interested handlers react — without the aggregate knowing about them.

## The Problem (API 1–4)

When a note is deleted, its votes should be cleaned up. In API 4:

```csharp
// RetroBoardService has to know about votes — cross-aggregate coupling
public async Task RemoveNote(Guid retroId, Guid columnId, Guid noteId, CancellationToken ct)
{
    var retro = await _retroRepository.GetByIdAsync(retroId, ct);
    retro.RemoveNote(columnId, noteId);

    // Service has to explicitly clean up votes
    var votes = await _voteRepository.GetByNoteIdAsync(noteId, ct);
    foreach (var vote in votes) _voteRepository.Delete(vote);

    await _unitOfWork.SaveChangesAsync(ct);
}
```

The RetroBoard service knows about the Vote aggregate — that's coupling.

## The Solution (API 5)

The aggregate raises an event. A separate handler reacts:

```csharp
// Aggregate — raises the event, doesn't know about votes
public void RemoveNote(Guid columnId, Guid noteId)
{
    var column = GetColumnOrThrow(columnId);
    column.RemoveNote(noteId);
    RaiseDomainEvent(new NoteRemovedEvent(noteId, columnId));
}

// Handler — reacts to the event, knows about votes
public class NoteRemovedEventHandler : INotificationHandler<NoteRemovedEvent>
{
    public async Task Handle(NoteRemovedEvent notification, CancellationToken ct)
    {
        var votes = await _voteRepository.GetByNoteIdAsync(notification.NoteId, ct);
        foreach (var vote in votes) _voteRepository.Delete(vote);
    }
}
```

## Dispatching Events

Events are dispatched by an EF Core interceptor **after `SaveChanges`** but
**within the same transaction**:

```csharp
public class DomainEventInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(...)
    {
        // Collect events from all modified aggregates
        var events = context.ChangeTracker.Entries<IHasDomainEvents>()
            .SelectMany(e => e.Entity.DomainEvents).ToList();

        // Clear events from entities
        // Publish each event via MediatR
        foreach (var domainEvent in events)
            await _publisher.Publish(domainEvent, ct);
    }
}
```

## Used In

| Tier | Domain Events? |
|------|---------------|
| API 1–4 | ❌ No |
| API 5 | ✅ Yes |
