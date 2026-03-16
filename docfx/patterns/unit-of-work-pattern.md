# Unit of Work Pattern

## Intent

Track all changes made during a business transaction and flush them to the
database in a **single atomic operation**.

## The Pattern

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class UnitOfWork : IUnitOfWork
{
    private readonly RetroBoardDbContext _context;

    public Task<int> SaveChangesAsync(CancellationToken ct)
        => _context.SaveChangesAsync(ct);
}
```

## Why Not Just Use DbContext Directly?

1. **Abstraction** — Application layer depends on `IUnitOfWork`, not `DbContext`.
2. **Explicit boundaries** — The call to `SaveChangesAsync` marks where the
   transaction happens.
3. **Single responsibility** — Repos manage entities, UoW manages transactions.

> **Counter-example: API 0** uses `DbContext.SaveChangesAsync()` directly in
> each endpoint handler — no `IUnitOfWork` abstraction. This works because
> the Transaction Script pattern has no separate application layer that needs
> to be decoupled from the database. Each handler IS the transaction. See
> [Transaction Script Pattern](transaction-script-pattern.md).

## Used in All APIs

The Unit of Work pattern is consistent across all five tiers. What changes
is **who calls it**:

| Tier | Called by |
|------|----------|
| API 1–4 | Service methods |
| API 5 | Command handlers (writes only; query handlers never save) |

## Coexistence with TransactionBehavior (API 5)

In API 5, the [TransactionBehavior](transaction-behavior.md) opens an explicit
database transaction before the command handler runs. This raises a natural
question: doesn't the TransactionBehavior replace the Unit of Work?

**No.** They operate at different levels:

| Concern | Who handles it |
|---------|---------------|
| "Flush tracked changes to the database" | **UnitOfWork** (`SaveChangesAsync`) |
| "Ensure everything is atomic" | **TransactionBehavior** (`BeginTransaction` / `Commit`) |

When an explicit transaction is already open, EF Core's `SaveChangesAsync`
does **not** create its own implicit transaction — it flushes changes within
the existing one. So the UnitOfWork continues calling `SaveChangesAsync`
exactly as before; those calls just participate in the transaction that
`TransactionBehavior` opened.

```
TransactionBehavior opens transaction
  │
  ├─ Handler calls UnitOfWork.SaveChangesAsync()
  │   → EF flushes without committing (transaction is still open)
  │
  ├─ DomainEventInterceptor dispatches events
  │   └─ Event handler calls UnitOfWork.SaveChangesAsync()
  │       → EF flushes without committing
  │
  └─ TransactionBehavior calls CommitAsync()
      → All flushed changes are committed together
```

The UnitOfWork code is **identical** across all five APIs — it doesn't know
or care about transactions. The pipeline behavior is the new layer on top.
