# Repositories

## What Is a Repository?

A **repository** is an abstraction that mediates between the domain layer and
the data mapping layer (ORM, database). It provides a **collection-like
interface** for accessing domain objects, hiding the details of how data is
persisted and retrieved.

> Think of a repository as a "domain-oriented collection" — you ask it for
> objects by their identity or criteria, and it returns domain entities. You
> never write SQL or call `DbContext` directly from your service layer.

## What a Repository Is NOT

This is where many teams go wrong:

- ❌ A repository is **not** a thin wrapper around `DbSet<T>`. If your
  repository just exposes `Add`, `GetById`, `GetAll`, `Update`, `Delete` with
  no domain-specific queries, you've gained nothing over using `DbContext`
  directly.

- ❌ A repository is **not** a place for business logic. It should not
  validate invariants or make decisions.

- ❌ A repository is **not** responsible for saving changes. That's the
  [Unit of Work](unit-of-work.md).

## Repository Evolution Across APIs

### API 1 & 2: Per-Entity Repositories

```csharp
public interface IColumnRepository
{
    Task<Column?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Column>> GetByRetroBoardIdAsync(Guid retroBoardId, CancellationToken ct = default);
    Task<bool> ExistsByNameInRetroAsync(Guid retroBoardId, string name, CancellationToken ct = default);
    Task AddAsync(Column column, CancellationToken ct = default);
    void Update(Column column);
    void Delete(Column column);
}
```

**The problem:** With a repository per entity, you end up with 7 repositories
for 7 entities. Cross-entity operations (e.g., "add a column to a retro board")
require coordinating multiple repositories, and there's no guarantee the
operation is atomic.

### API 3+: Per-Aggregate Repositories

```csharp
// Only TWO repository interfaces for the entire retro domain
public interface IRetroBoardRepository
{
    Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RetroBoard board, CancellationToken ct = default);
    void Delete(RetroBoard board);
}
```

The repository loads the **entire aggregate** — the retro board with all its
columns, notes, and votes. This ensures the aggregate root has full state to
enforce invariants.

### API 5: Repositories Are Write-Side Only (CQRS)

In API 5, repositories are only used by **command handlers** (writes).
**Query handlers** bypass repositories entirely and read from `DbContext`
directly using no-tracking projections:

```csharp
// Command handler — uses the repository (write model)
var retro = await _repository.GetByIdAsync(id, ct);
retro.AddColumn("New Column");
await _unitOfWork.SaveChangesAsync(ct);

// Query handler — uses DbContext directly (read model)
var result = await _context.RetroBoards
    .AsNoTracking()
    .Where(r => r.Id == id)
    .Select(r => new RetroBoardResponse { ... })
    .FirstOrDefaultAsync(ct);
```

## Where Repository Interfaces Live

| API Tier | Interface Location | Implementation Location |
|----------|-------------------|------------------------|
| API 1–2 | Application layer (`Interfaces/`) | Infrastructure (`Repositories/`) |
| API 3–5 | **Domain layer** (`IRetroBoardRepository.cs`) | Infrastructure (`Repositories/`) |

Moving the interface to the Domain layer (API 3+) follows the **Dependency
Inversion Principle** — the domain defines the contract, infrastructure
provides the implementation.

## The "Repository Per Entity" Trap

API 1 has 7 repositories. API 3 has 3. API 4 has 4 (because Vote became its
own aggregate). The number of repositories should match the number of
**aggregates**, not the number of **tables**.

If you find yourself with a repository per database table, you're essentially
building a table-access layer — not a domain-oriented abstraction.

## Where to Go Next

- [Unit of Work](unit-of-work.md) — How saving is coordinated.
- [Aggregates](aggregates.md) — Why aggregate-level repositories are the right
  granularity.
