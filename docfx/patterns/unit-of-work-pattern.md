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

## Used in All APIs

The Unit of Work pattern is consistent across all five tiers. What changes
is **who calls it**:

| Tier | Called by |
|------|----------|
| API 1–4 | Service methods |
| API 5 | Command handlers (writes only; query handlers never save) |
