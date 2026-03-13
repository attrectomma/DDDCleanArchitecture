# Services & Orchestration

## What Is a Service?

In Clean Architecture, a **service** (or application service) lives in the
Application layer and orchestrates use cases. It's the "glue" between the
outside world (controllers, message handlers) and the domain.

A well-designed service does three things:

1. **Load** — Fetch the entities/aggregates needed for the operation.
2. **Delegate** — Call domain methods that enforce business rules.
3. **Persist** — Save changes via the Unit of Work.

A well-designed service does **not**:

- ❌ Contain business logic (that belongs in the domain).
- ❌ Know about HTTP, controllers, or serialization.
- ❌ Call other services (that creates hidden coupling).

## Service Evolution Across APIs

### API 1: Services Own Everything

In API 1, services are fat. They contain all the business logic because the
entities are anemic:

```csharp
// API 1 — Service does validation, invariant checks, mapping, AND persistence
public class ColumnService : IColumnService
{
    public async Task<ColumnResponse> CreateAsync(
        Guid retroBoardId, CreateColumnRequest request, CancellationToken ct)
    {
        var retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, ct)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // Business rule enforced HERE in the service
        if (await _columnRepository.ExistsByNameInRetroAsync(retroBoardId, request.Name, ct))
            throw new DuplicateException("Column", "Name", request.Name);

        var column = new Column { RetroBoardId = retroBoardId, Name = request.Name };
        await _columnRepository.AddAsync(column, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToResponse(column);
    }
}
```

### API 2: Services Become Thin Orchestrators

With rich domain models, the service delegates to the entity:

```csharp
// API 2 — Service orchestrates; entity enforces within-entity rules
public class NoteService : INoteService
{
    public async Task<VoteResponse> CastVoteAsync(
        Guid noteId, CastVoteRequest request, CancellationToken ct)
    {
        var note = await _noteRepository.GetByIdWithVotesAsync(noteId, ct)
            ?? throw new NotFoundException("Note", noteId);

        var vote = note.CastVote(request.UserId);  // Entity enforces one-vote-per-user

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToResponse(vote);
    }
}
```

### API 3–4: One Service Per Aggregate

With aggregate design, you need far fewer services:

| API | Service Count | Reason |
|-----|--------------|--------|
| API 1 | 7 | One per entity |
| API 2 | 7 | Same structure, thinner logic |
| API 3 | 3 | One per aggregate (User, Project, RetroBoard) |
| API 4 | 4 | Vote extracted as own aggregate |

### API 5: No Services — Command/Query Handlers

In API 5, services are replaced by MediatR handlers. Each use case gets its
own handler class:

```csharp
// API 5 — No "ColumnService". Each operation is a focused handler.
public class AddColumnCommandHandler : IRequestHandler<AddColumnCommand, ColumnResponse>
{
    public async Task<ColumnResponse> Handle(AddColumnCommand command, CancellationToken ct)
    {
        var retro = await _repository.GetByIdAsync(command.RetroBoardId, ct)
            ?? throw new NotFoundException("RetroBoard", command.RetroBoardId);

        var column = retro.AddColumn(command.Name);
        await _unitOfWork.SaveChangesAsync(ct);

        return new ColumnResponse(column.Id, column.Name, null);
    }
}
```

## The "God Service" Anti-Pattern

If your service class is hundreds of lines long with dozens of methods, you've
built a **God Service**. This happens naturally when:

- Entities are anemic (all logic moves to services)
- You have one service per entity (the service becomes a dumping ground)
- Cross-cutting concerns (logging, validation, auth) are inline

The migration path in this repository shows how to decompose God Services:

1. **API 2:** Move entity-specific logic into the entities.
2. **API 3:** Consolidate per-entity services into per-aggregate services.
3. **API 5:** Replace services entirely with focused command/query handlers.

## Where to Go Next

- [DTOs](dtos.md) — What goes in and out of services.
- [Unit of Work](unit-of-work.md) — How services coordinate persistence.
