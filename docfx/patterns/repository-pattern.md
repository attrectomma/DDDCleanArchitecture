# Repository Pattern

## Intent

Provide a **collection-like interface** for accessing domain objects,
decoupling the domain from the data access technology.

## The Pattern

```csharp
// The domain says "I need this"
public interface IRetroBoardRepository
{
    Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(RetroBoard board, CancellationToken ct);
    void Delete(RetroBoard board);
}

// Infrastructure says "I provide this"
public class RetroBoardRepository : IRetroBoardRepository
{
    private readonly RetroBoardDbContext _context;

    public async Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _context.RetroBoards
            .Include(r => r.Columns).ThenInclude(c => c.Notes)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
}
```

## Evolution Across APIs

| Tier | Granularity | Count | Repository knows about |
|------|------------|-------|----------------------|
| API 1–2 | Per entity | 7 | Single entity + basic queries |
| API 3–4 | Per aggregate | 3–4 | Full aggregate graph (Include chains) |
| API 5 | Per aggregate (write side) | 3–4 | Full aggregate graph; reads bypass repos |

## Common Mistakes

1. **Generic repository anti-pattern** — A `Repository<T>` base class that
   exposes `IQueryable<T>`. This leaks EF Core abstractions into the domain.

2. **Repository per table** — If every table has a repository, you've built
   a table-access layer, not a domain abstraction.

3. **SaveChanges in repositories** — Repositories manage entity access, not
   transactions. See [Unit of Work](unit-of-work-pattern.md).

## When to Use

Always, in any non-trivial application. The debate is about **granularity**
(per-entity vs per-aggregate) and **scope** (all operations vs write-side only).
