# Transaction Behavior (Pipeline Pattern)

## Intent

Ensure that a command handler and all its side effects (including domain
event handlers) execute within a **single atomic database transaction**.
Either everything commits, or everything rolls back.

## The Problem

Consider the `RemoveNote` flow in API 5:

```
1. RemoveNoteCommandHandler calls SaveChangesAsync()  → note marked deleted
2. DomainEventInterceptor fires (SavedChangesAsync hook)
3. NoteRemovedEventHandler loads and deletes orphaned votes
4. NoteRemovedEventHandler calls SaveChangesAsync()   → votes marked deleted
```

Without an explicit transaction, steps 1 and 4 are **two separate implicit
transactions**. EF Core wraps each `SaveChangesAsync` call in its own
transaction by default. If step 4 fails (e.g., database timeout), step 1
has already committed:

```
✅ Note deleted  (committed in step 1)
❌ Votes remain  (step 4 rolled back or never ran)
```

This is **inconsistent state** — the note is gone but its votes are orphaned.

## The Solution

The `TransactionBehavior` wraps the **entire command pipeline** — including
domain event handlers triggered during save — in a single explicit
`BeginTransactionAsync()` / `CommitAsync()` scope:

```
BeginTransaction
  ├─ RemoveNoteCommandHandler.Handle()
  │    └─ SaveChangesAsync() ← no implicit commit (transaction already open)
  │         └─ DomainEventInterceptor fires
  │              └─ NoteRemovedEventHandler.Handle()
  │                   └─ SaveChangesAsync() ← still in the same transaction
  └─ CommitAsync()     ← single atomic commit of both saves
```

If anything fails at any point, the entire operation rolls back.

## Implementation

```csharp
public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>  // ← only commands, not queries
{
    private readonly DbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Re-entrancy guard: if a transaction is already active, delegate directly
        if (_dbContext.Database.CurrentTransaction is not null)
            return await next();

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            TResponse response = await next();
            await transaction.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

### Key Design Decisions

#### 1. The `ICommand<T>` Marker Interface

The `where TRequest : ICommand<TResponse>` generic constraint ensures this
behavior **only activates for commands**. Queries implement `IRequest<T>`
directly and bypass the transaction entirely — they are read-only and never
call `SaveChangesAsync`.

```csharp
// Command — wrapped in transaction
public record AddColumnCommand(Guid RetroBoardId, string Name)
    : ICommand<ColumnResponse>;

// Query — no transaction
public record GetRetroBoardQuery(Guid RetroBoardId)
    : IRequest<RetroBoardResponse>;
```

This makes the CQRS read/write split explicit at the type system level.
MediatR's generic pipeline matches `ICommand<T>` because it extends
`IRequest<T>`, so all existing `IRequestHandler<T>` implementations continue
to work unchanged.

#### 2. The Re-entrancy Guard

```csharp
if (_dbContext.Database.CurrentTransaction is not null)
    return await next();
```

If a domain event handler triggers another command (or another save), the
inner `TransactionBehavior` detects that a transaction is already open and
delegates directly. The **outermost** behavior owns the commit/rollback.
This prevents nested transaction errors.

#### 3. Injecting `DbContext` (Not the Concrete Type)

The behavior injects `DbContext` (the EF Core base class), not
`RetroBoardDbContext`. This keeps the Application layer free of Infrastructure
references. The DI container resolves it to the registered concrete type:

```csharp
// Program.cs
builder.Services.AddScoped<DbContext>(sp =>
    sp.GetRequiredService<RetroBoardDbContext>());
```

## How It Coexists with Unit of Work

This is the most common question. The answer: **they operate at different
levels and do not conflict.**

| Concern | Who handles it |
|---------|---------------|
| "Flush tracked changes to the database" | **UnitOfWork** (`SaveChangesAsync`) |
| "Ensure everything is atomic" | **TransactionBehavior** (`BeginTransaction` / `Commit`) |

The Unit of Work calls `SaveChangesAsync`. When an explicit transaction is
already open, EF Core's `SaveChangesAsync` does **not** create its own
implicit transaction — it flushes changes within the existing one.

```
TransactionBehavior opens transaction
  │
  ├─ Handler calls UnitOfWork.SaveChangesAsync()
  │   → EF Core detects existing transaction, flushes without committing
  │
  ├─ DomainEventInterceptor dispatches events
  │   └─ Event handler calls UnitOfWork.SaveChangesAsync()
  │       → EF Core detects existing transaction, flushes without committing
  │
  └─ TransactionBehavior calls CommitAsync()
      → All flushed changes are committed together
```

The UnitOfWork is completely unaware of the transaction. Its code stays
exactly the same as API 1–4:

```csharp
public class UnitOfWork : IUnitOfWork
{
    private readonly RetroBoardDbContext _context;

    public Task<int> SaveChangesAsync(CancellationToken ct)
        => _context.SaveChangesAsync(ct);
}
```

## Pipeline Order

```
Request → LoggingBehavior → ValidationBehavior → TransactionBehavior → Handler
```

The order is intentional:

1. **LoggingBehavior** runs first — the request is logged even if validation
   rejects it.
2. **ValidationBehavior** runs second — we never open a database transaction
   for a request that will be rejected.
3. **TransactionBehavior** runs third — wraps only valid commands in a
   transaction.

Queries skip step 3 entirely because they don't implement `ICommand<T>`.

## Registration

```csharp
// Program.cs — order matters
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

// DbContext base class registration (so TransactionBehavior can inject it)
builder.Services.AddScoped<DbContext>(sp =>
    sp.GetRequiredService<RetroBoardDbContext>());
```

## Comparison Across Tiers

| Tier | Transaction management |
|------|----------------------|
| API 1–4 | Implicit — each `SaveChangesAsync` is its own transaction |
| API 5 | **Explicit** — `TransactionBehavior` wraps commands; domain event handlers participate |

In API 1–4, this was never a problem because there were no domain events —
each service method made a single `SaveChangesAsync` call. API 5's domain
events introduce the possibility of multiple saves per request, making
explicit transaction management necessary.

## Trade-offs

| Pro | Con |
|-----|-----|
| Atomic operations across handler + event handlers | Longer-lived transactions (hold locks longer) |
| Consistent state guaranteed | One more pipeline behavior to understand |
| Transparent to handlers (no code change needed) | Requires `DbContext` base class DI registration |
| Re-entrancy safe | Must understand EF Core implicit vs explicit transactions |

## See Also

- [Unit of Work Pattern](unit-of-work-pattern.md) — The "flush changes"
  pattern that coexists with TransactionBehavior
- [Mediator Pattern](mediator.md) — Pipeline behaviors and how they compose
- [Domain Events](domain-events.md) — Why multiple saves happen in one request
- [CQRS](cqrs.md) — The `ICommand<T>` vs `IRequest<T>` type-level separation
