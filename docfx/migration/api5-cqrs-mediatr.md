# API 5 — CQRS + MediatR

> **Pattern:** Command/Query Responsibility Segregation with MediatR.
> Behavior-centric handlers replace noun-centric services. Domain events
> decouple side effects. Strategy + Specification patterns enable configurable
> voting rules.

## What Changes from API 4

| Aspect | API 4 | API 5 |
|--------|-------|-------|
| Application layer | Services per aggregate | **Command/Query handlers per use case** |
| Read path | Loads full aggregate | **DbContext projections (no tracking)** |
| Write path | Loads aggregate via repo | Same — aggregate via repo |
| Cross-cutting | Inline in services | **Pipeline behaviors** |
| Side effects | Inline | **Domain events** |
| Controller deps | Per-service | **IMediator only** |
| Voting rules | Hardcoded one-vote-per-note | **Configurable via Strategy + Specification** |
| Vote uniqueness | DB unique constraint | **Application-level specification** |

## CQRS — The Core Idea

**Commands** (writes) and **Queries** (reads) have fundamentally different
requirements:

| | Command (Write) | Query (Read) |
|---|---|---|
| Needs aggregate? | ✅ Yes — for invariant enforcement | ❌ No — just need data |
| Change tracking? | ✅ Yes — EF needs to detect changes | ❌ No — wasted overhead |
| Full graph? | ✅ Yes — aggregate needs complete state | ❌ No — project only needed fields |
| Concurrency? | ✅ Yes — optimistic locking | ❌ No — reads don't conflict |

API 5 separates them:

```csharp
// WRITE — Command handler loads the aggregate
public class AddColumnCommandHandler : IRequestHandler<AddColumnCommand, ColumnResponse>
{
    private readonly IRetroBoardRepository _repository;  // aggregate repo

    public async Task<ColumnResponse> Handle(AddColumnCommand command, CancellationToken ct)
    {
        var retro = await _repository.GetByIdAsync(command.RetroBoardId, ct);
        var column = retro.AddColumn(command.Name);
        await _unitOfWork.SaveChangesAsync(ct);
        return new ColumnResponse(column.Id, column.Name, null);
    }
}

// READ — Query handler projects directly from DB
public class GetRetroBoardQueryHandler : IRequestHandler<GetRetroBoardQuery, RetroBoardResponse>
{
    private readonly RetroBoardDbContext _context;  // direct DbContext, NOT repo

    public async Task<RetroBoardResponse> Handle(GetRetroBoardQuery query, CancellationToken ct)
    {
        return await _context.RetroBoards
            .AsNoTracking()
            .Where(r => r.Id == query.RetroBoardId)
            .Select(r => new RetroBoardResponse { /* projection */ })
            .FirstOrDefaultAsync(ct);
    }
}
```

## MediatR Pipeline Behaviors

Cross-cutting concerns that were inline in services (API 1–4) are now
pipeline behaviors that run automatically:

```
Controller → IMediator.Send()
                │
                ├─ LoggingBehavior       (logs request/response)
                ├─ ValidationBehavior    (runs FluentValidation)
                ├─ TransactionBehavior   (wraps commands in DB transaction)
                │
                └─ Handler               (actual business logic)
```

Adding a new cross-cutting concern (e.g., authorization) means adding one
pipeline behavior — not modifying every service method.

## Domain Events

Aggregates raise events. Other parts of the system react:

```csharp
// In the RetroBoard aggregate
public void RemoveNote(Guid columnId, Guid noteId)
{
    var column = GetColumnOrThrow(columnId);
    column.RemoveNote(noteId);
    RaiseDomainEvent(new NoteRemovedEvent(noteId, columnId));
}

// Somewhere else — a handler reacts
public class NoteRemovedEventHandler : INotificationHandler<NoteRemovedEvent>
{
    public async Task Handle(NoteRemovedEvent notification, CancellationToken ct)
    {
        // Clean up orphaned votes
        var votes = await _voteRepository.GetByNoteIdAsync(notification.NoteId, ct);
        foreach (var vote in votes)
            _voteRepository.Delete(vote);
    }
}
```

The retro aggregate doesn't know about votes. The vote cleanup is **decoupled**.

## Strategy + Specification Patterns

API 5 introduces two additional design patterns for voting rules:

### The Strategy Pattern — Configurable Voting

Each retro board has a `VotingStrategyType` property that determines how votes
are validated:

| Strategy | Behaviour |
|----------|-----------|
| **Default** | One vote per user per note (same as API 1–4) |
| **Budget** | Max 3 votes per user per column; multiple votes on same note allowed ("dot voting") |

The `CastVoteCommandHandler` resolves the strategy from the board's configuration
and delegates all vote validation to it — zero conditional branching:

```csharp
IVotingStrategy strategy = VotingStrategyFactory.Create(retro.VotingStrategyType);
strategy.Validate(eligibilityContext);
```

### The Specification Pattern — Composable Rules

Each voting rule is a standalone specification that evaluates a `VoteEligibilityContext`:

```csharp
// Each specification answers one question
public class UniqueVotePerNoteSpecification : ISpecification<VoteEligibilityContext>
{
    public bool IsSatisfiedBy(VoteEligibilityContext ctx) =>
        !ctx.UserAlreadyVotedOnNote;
}
```

Strategies compose specifications differently:

```csharp
// Default: NoteExists AND IsMember AND UniqueVote
// Budget:  NoteExists AND IsMember AND BudgetNotExceeded
```

The Budget strategy intentionally **omits** `UniqueVotePerNoteSpecification` and
**adds** `VoteBudgetNotExceededSpecification`. This difference in composition is
the educational payoff of combining Strategy + Specification.

### DB Constraint Trade-off

The API 4 unique index on `(NoteId, UserId)` was removed because the Budget
strategy allows duplicate votes. The Default strategy enforces uniqueness at
the application level. Under extreme concurrency, this creates a (rare) race
condition risk — documented as an intentional trade-off.

For full details, see:
- [Specification Pattern](../patterns/specification-pattern.md)
- [Strategy Pattern](../patterns/strategy-pattern.md)

## Trade-offs

| Trade-off | Description |
|-----------|------------|
| **More files** | ~50 files in the Application layer (13 commands × 3 + queries + handlers) |
| **Indirection** | Controller → MediatR → Pipeline → Handler is harder to trace |
| **Two data paths** | Command handlers use repos; query handlers use DbContext |
| **Learning curve** | MediatR, pipeline behaviors, CQRS, domain events |
| **"CQRS lite"** | Same database for reads and writes (no read replica) |

## When to Use This Level of Architecture

API 5's architecture shines when:
- The team is growing and features are added frequently
- Read/write ratios are highly skewed (many reads, few writes)
- Cross-cutting concerns are numerous (logging, auth, validation, caching)
- You need to add reactions to domain events without modifying existing code

For a small CRUD app with one developer, this is over-engineering. And that's
OK — knowing *when* to apply a pattern is as important as knowing *how*.
