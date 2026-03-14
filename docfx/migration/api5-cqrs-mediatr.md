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
| Vote uniqueness | DB unique constraint | **Conditional — DB unique index (Default) or app-level only (Budget)** |
| Configuration | Hardcoded constants | **Options pattern (`IOptions<VotingOptions>`) + startup validation** |

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
and delegates all vote validation to it — zero conditional branching. The budget
limit is sourced from `VotingOptions` via the [Options pattern](../patterns/options-pattern.md):

```csharp
IVotingStrategy strategy = VotingStrategyFactory.Create(
    retro.VotingStrategyType, _votingOptions.MaxVotesPerColumn);
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

The API 4 unique index on `(NoteId, UserId)` is now **conditional** — controlled
by the [Options pattern](../patterns/options-pattern.md). When
`VotingOptions.DefaultVotingStrategy` is `Default`, the `RetroBoardDbContext`
applies a **unique** index as a DB safety net. When set to `Budget`, the index
is **non-unique** because dot voting allows multiple votes per user per note.

| Config | Unique index? | Race condition risk |
|--------|--------------|--------------------|
| Default | ✅ Yes — DB catches concurrent duplicates | None |
| Budget | ❌ No — specification only | Rare, under extreme concurrency |

This conditional schema is powered by `IOptions<VotingOptions>` injected into
`RetroBoardDbContext`, with a custom `IModelCacheKeyFactory` to ensure EF Core
builds separate models per configuration.

For full details, see:
- [Options Pattern](../patterns/options-pattern.md)
- [Specification Pattern](../patterns/specification-pattern.md)
- [Strategy Pattern](../patterns/strategy-pattern.md)

## Options Pattern — Externalized Configuration

API 5 uses the **Options pattern** (`IOptions<T>`) to externalize voting
configuration that was hardcoded in API 1–4.

### VotingOptions

```csharp
public class VotingOptions
{
    public const string SectionName = "Voting";
    public VotingStrategyType DefaultVotingStrategy { get; set; } = VotingStrategyType.Default;
    public int MaxVotesPerColumn { get; set; } = 3;
}
```

### Startup Validation

A dedicated `VotingOptionsValidator` implements `IValidateOptions<VotingOptions>`
and runs at startup via `.ValidateOnStart()`. Invalid configuration (unknown
enum value, non-positive budget) **crashes the app immediately** instead of
failing silently at runtime:

```csharp
builder.Services
    .AddOptions<VotingOptions>()
    .Bind(builder.Configuration.GetSection(VotingOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<VotingOptions>, VotingOptionsValidator>();
```

### Where Options Flow

| Consumer | What it uses | Why |
|----------|-------------|-----|
| `CreateRetroBoardCommandHandler` | `DefaultVotingStrategy` | Fallback when caller doesn't specify a strategy |
| `CastVoteCommandHandler` | `MaxVotesPerColumn` | Passes budget limit to `VotingStrategyFactory` |
| `RetroBoardDbContext` | `DefaultVotingStrategy` | Conditionally applies unique index on `Vote(NoteId, UserId)` |

### appsettings.json

```json
{
  "Voting": {
    "DefaultVotingStrategy": "Default",
    "MaxVotesPerColumn": 3
  }
}
```

Changing `DefaultVotingStrategy` to `"Budget"` switches the entire application
to dot voting — no code change, no redeployment of binaries.

For the full deep-dive, see [Options Pattern](../patterns/options-pattern.md).

## Trade-offs

| Trade-off | Description |
|-----------|------------|
| **More files** | ~50 files in the Application layer (13 commands × 3 + queries + handlers) |
| **Indirection** | Controller → MediatR → Pipeline → Handler is harder to trace |
| **Two data paths** | Command handlers use repos; query handlers use DbContext |
| **Learning curve** | MediatR, pipeline behaviors, CQRS, domain events, Options pattern |
| **"CQRS lite"** | Same database for reads and writes (no read replica) |
| **Config-dependent schema** | Conditional unique index requires custom `IModelCacheKeyFactory` |

## When to Use This Level of Architecture

API 5's architecture shines when:
- The team is growing and features are added frequently
- Read/write ratios are highly skewed (many reads, few writes)
- Cross-cutting concerns are numerous (logging, auth, validation, caching)
- You need to add reactions to domain events without modifying existing code

For a small CRUD app with one developer, this is over-engineering. And that's
OK — knowing *when* to apply a pattern is as important as knowing *how*.
