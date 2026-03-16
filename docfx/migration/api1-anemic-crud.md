# API 1 — Anemic CRUD

> **Pattern:** Table → Entity → Repository → Service → Controller (1-to-1)

> 💡 **See also:** Before diving into the layered approach, see
> [API 0 — Transaction Script](api0-transaction-script.md) for a single-project
> alternative that achieves the same REST contract with a fraction of the code.

## What This Tier Shows

This is the architecture you see in most junior codebases. It's not wrong for
small apps, but it becomes painful as the domain grows.

## Structure

```
Api1.Domain/           ← Anemic entities (property bags)
Api1.Application/      ← Services (ALL business logic), DTOs, Validators
Api1.Infrastructure/   ← DbContext, Repositories, Interceptors, UoW
Api1.WebApi/           ← Controllers, Middleware, Program.cs
```

Every database table has a matching entity, repository, service, and controller.
This 1-to-1 mapping is simple to understand but doesn't scale.

## How Business Rules Work

All invariant checks live in the service layer:

```csharp
// ColumnService.CreateAsync
if (await _columnRepository.ExistsByNameInRetroAsync(retroBoardId, request.Name, ct))
    throw new DuplicateException("Column", "Name", request.Name);

var column = new Column { RetroBoardId = retroBoardId, Name = request.Name };
await _columnRepository.AddAsync(column, ct);
await _unitOfWork.SaveChangesAsync(ct);
```

The entity itself is just a data container:

```csharp
public class Column : AuditableEntityBase
{
    public Guid RetroBoardId { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

## What's Wrong (intentionally)

| Problem | Description |
|---------|------------|
| **Race conditions** | Check-then-act: two requests can both pass the uniqueness check |
| **No concurrency control** | Last write wins silently |
| **Scattered rules** | Business logic spread across 7 service classes |
| **Anemic domain** | Entities carry no value beyond defining database shape |
| **No consistency boundary** | No guarantee that related data is consistent |

## Concurrency Test Results

```
✅ CRUD Happy Path           — Pass
✅ Invariant Enforcement     — Pass (single-threaded)
✅ Soft Delete               — Pass
❌ Concurrent Duplicate      — FAIL (both requests succeed)
❌ Concurrent Vote           — FAIL (both votes created)
```

## What Changes in API 2

→ [API 2 — Rich Domain](api2-rich-domain.md): Business logic moves from
services into entities. Entities get private setters, constructors, and
methods that enforce invariants.
