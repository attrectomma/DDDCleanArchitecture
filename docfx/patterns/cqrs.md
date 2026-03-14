# CQRS — Command Query Responsibility Segregation

## Intent

Separate the **read model** (queries) from the **write model** (commands)
because they have fundamentally different requirements.

## The Problem CQRS Solves

In API 3 and 4, the same repository and aggregate are used for both reads
and writes:

```csharp
// READ — loads the full aggregate just to return a DTO
var retro = await _repository.GetByIdAsync(id, ct);  // heavy query
return MapToResponse(retro);                          // immediate mapping

// WRITE — loads the full aggregate to enforce invariants
var retro = await _repository.GetByIdAsync(id, ct);   // same heavy query
retro.AddColumn("New Column");                         // domain logic
await _unitOfWork.SaveChangesAsync(ct);
```

For reads, we're paying the cost of:
- Full aggregate graph loading (Include chains)
- EF Core change tracking (unnecessary for reads)
- Object hydration (building entity objects we immediately convert to DTOs)

## How API 5 Implements CQRS

### Type-Level Separation: `ICommand<T>` vs `IRequest<T>`

The read/write split is enforced at the type system level using a marker
interface:

```csharp
// ICommand<T> extends IRequest<T> — commands ARE requests,
// but with a marker that pipeline behaviors can detect.
public interface ICommand<out TResponse> : IRequest<TResponse>;
```

Commands implement `ICommand<T>`:

```csharp
public record AddColumnCommand(Guid RetroBoardId, string Name)
    : ICommand<ColumnResponse>;
```

Queries implement `IRequest<T>` directly:

```csharp
public record GetRetroBoardQuery(Guid RetroBoardId)
    : IRequest<RetroBoardResponse>;
```

This distinction enables the [TransactionBehavior](transaction-behavior.md)
to wrap only commands in a database transaction — queries skip it entirely.
Without this marker, the pipeline would need naming conventions or folder
structure to distinguish commands from queries — a fragile approach.

### Write Side (Commands)

Commands flow through aggregate roots — same as API 4:

```csharp
public class AddColumnCommandHandler : IRequestHandler<AddColumnCommand, ColumnResponse>
{
    private readonly IRetroBoardRepository _repository;  // aggregate repo

    public async Task<ColumnResponse> Handle(AddColumnCommand cmd, CancellationToken ct)
    {
        var retro = await _repository.GetByIdAsync(cmd.RetroBoardId, ct);
        var column = retro.AddColumn(cmd.Name);
        await _unitOfWork.SaveChangesAsync(ct);
        return new ColumnResponse(column.Id, column.Name, null);
    }
}
```

### Read Side (Queries)

Queries bypass aggregates and project directly from the database:

```csharp
public class GetRetroBoardQueryHandler : IRequestHandler<GetRetroBoardQuery, RetroBoardResponse>
{
    private readonly RetroBoardDbContext _context;  // direct DbContext, NOT repo

    public async Task<RetroBoardResponse> Handle(GetRetroBoardQuery query, CancellationToken ct)
    {
        return await _context.RetroBoards
            .AsNoTracking()                        // no change tracking overhead
            .Where(r => r.Id == query.RetroBoardId)
            .Select(r => new RetroBoardResponse    // projection at the DB level
            {
                Id = r.Id,
                Name = r.Name,
                Columns = r.Columns.Select(c => new ColumnResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    Notes = c.Notes.Select(n => new NoteResponse
                    {
                        Id = n.Id,
                        Text = n.Text,
                        VoteCount = _context.Set<Vote>().Count(v => v.NoteId == n.Id)
                    }).ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);
    }
}
```

## "CQRS Lite"

This repository uses **CQRS Lite** — the same database for reads and writes,
with separation only at the code level. Full CQRS might use:
- Separate read databases (denormalized, optimized for queries)
- Event sourcing (store events, not state)
- Materialized views

These are out of scope for this educational repo, but the code-level
separation is the essential first step.

## When to Use CQRS

✅ Good fit when:
- Read and write workloads are very different
- Reads are far more frequent than writes
- You need different data shapes for reading vs writing
- Your aggregates are expensive to load

❌ Overkill when:
- Simple CRUD with low traffic
- Reads and writes are equally simple
- Small team, few entities
