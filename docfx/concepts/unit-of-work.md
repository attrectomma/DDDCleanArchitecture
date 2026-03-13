# Unit of Work

## What Is the Unit of Work Pattern?

The **Unit of Work** pattern tracks all changes made during a business
transaction and coordinates writing them to the database in a single atomic
operation. It ensures that either **all changes succeed** or **none do**.

In EF Core, `DbContext` already implements this pattern internally —
`SaveChangesAsync()` flushes all tracked changes in one database transaction.
The `IUnitOfWork` interface makes this explicit in the architecture.

## Why Wrap DbContext?

You might ask: "If `DbContext.SaveChangesAsync()` already exists, why create
`IUnitOfWork`?"

1. **Abstraction** — The Application layer depends on `IUnitOfWork`, not on
   `DbContext`. This keeps the domain and application layers free of EF Core
   references.

2. **Single responsibility** — Repositories manage entity access. The Unit of
   Work manages transaction boundaries. Separating these prevents repositories
   from calling `SaveChanges` at arbitrary points.

3. **Explicit intent** — `await _unitOfWork.SaveChangesAsync(ct)` at the end
   of a service method makes the transaction boundary visible.

## Implementation

```csharp
// Interface — lives in Application layer (API 1-2) or Domain layer (API 3+)
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Implementation — lives in Infrastructure layer
public class UnitOfWork : IUnitOfWork
{
    private readonly RetroBoardDbContext _context;

    public UnitOfWork(RetroBoardDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
```

## The Rule: Only UnitOfWork Calls SaveChanges

This is a critical rule in the RetroBoard codebase:

> **Repositories NEVER call `SaveChanges`.** Only the `UnitOfWork` does.

Why? Because a single use case may involve multiple repository operations:

```csharp
// Service method — one unit of work spanning multiple operations
public async Task TransferNote(Guid fromColumnId, Guid toColumnId, Guid noteId, CancellationToken ct)
{
    var fromColumn = await _columnRepository.GetByIdAsync(fromColumnId, ct);
    var toColumn = await _columnRepository.GetByIdAsync(toColumnId, ct);

    fromColumn.RemoveNote(noteId);
    toColumn.AddNote(noteText);

    // Both changes are saved atomically
    await _unitOfWork.SaveChangesAsync(ct);
}
```

If repositories called `SaveChanges` internally, the first removal could
succeed while the second addition fails — leaving the system in an
inconsistent state.

## Where to Go Next

- [Repositories](repositories.md) — The other half of the persistence equation.
- [Consistency Boundaries](consistency-boundaries.md) — How the scope of a
  Unit of Work relates to aggregate boundaries.
