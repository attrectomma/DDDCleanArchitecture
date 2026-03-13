# API 3 — Aggregate Design

> **Pattern:** Two aggregates (Project, RetroBoard). One repository and one
> service per aggregate. Optimistic concurrency via PostgreSQL `xmin`.

## What Changes from API 2

| Aspect | API 2 | API 3 |
|--------|-------|-------|
| Repository count | 7 (per entity) | **3** (per aggregate) |
| Service count | 7 | **3** |
| Cross-entity invariants | Service layer | **Aggregate root methods** |
| Concurrency | None | **Optimistic (xmin)** |
| Loading strategy | Ad-hoc Includes | **Aggregate repo always loads full graph** |

## The Aggregate Design

**RetroBoard aggregate** owns Columns → Notes → Votes:

```csharp
public class RetroBoard : AuditableEntityBase, IAggregateRoot
{
    private readonly List<Column> _columns = new();

    public Column AddColumn(string name)
    {
        if (_columns.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"Column name '{name}' already exists.");

        var column = new Column(Id, name);
        _columns.Add(column);
        return column;
    }

    public Vote CastVote(Guid columnId, Guid noteId, Guid userId)
    {
        var column = GetColumnOrThrow(columnId);
        var note = column.GetNoteOrThrow(noteId);
        return note.CastVote(userId);
    }
}
```

The **repository loads the entire aggregate**:

```csharp
return await _context.RetroBoards
    .Include(r => r.Columns)
        .ThenInclude(c => c.Notes)
            .ThenInclude(n => n.Votes)
    .FirstOrDefaultAsync(r => r.Id == id, ct);
```

## What This Solves

- ✅ **Cross-entity invariants** — Column name uniqueness enforced by the
  aggregate root, which sees all columns.
- ✅ **No loading strategy coupling** — The aggregate repo always loads the
  full graph, so domain checks always have complete data.
- ✅ **Optimistic concurrency** — `xmin` concurrency token on the aggregate
  root prevents concurrent modifications.
- ✅ **Fewer classes** — 3 repositories instead of 7, 3 services instead of 7.

## Concurrency Test Results

```
✅ CRUD Happy Path           — Pass
✅ Invariant Enforcement     — Pass
✅ Soft Delete               — Pass
✅ Concurrent Duplicate      — Pass (second request gets 409)
✅ Concurrent Vote           — Pass (second request gets 409)
```

## The Aggregate Explosion Problem

The RetroBoard aggregate is **too large**. A retro with 5 columns, 50 notes,
and 200 votes loads 255 entities for every single operation — including a
simple vote.

Worse: **any write** to the aggregate conflicts with **any other write** to
the same aggregate. Two users voting on different notes in the same retro
get a concurrency conflict, even though their changes don't logically overlap.

## What Changes in API 4

→ [API 4 — Split Aggregates](api4-split-aggregates.md): Vote is extracted as
its own aggregate. The RetroBoard aggregate shrinks. Write contention drops.
But cross-aggregate invariant enforcement becomes more complex.
