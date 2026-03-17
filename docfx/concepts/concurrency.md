# Concurrency Control

## The Problem

When two users modify the same data at the same time, one of three things
can happen:

1. **Last write wins** (silent data loss) — API 0a, API 1 & 2
2. **Optimistic concurrency** (detect and reject) — API 0b (DB-level), API 3, 4, & 5
3. **Pessimistic locking** (prevent concurrent access) — Not used in this repo

## Last Write Wins (API 1 & 2)

Without concurrency control, EF Core generates an `UPDATE` statement that
overwrites whatever is in the database:

```sql
-- User A and User B both loaded column with Name = "Old Name"
-- User A saves first
UPDATE "Columns" SET "Name" = 'Name A' WHERE "Id" = '...';

-- User B saves second — silently overwrites User A's change
UPDATE "Columns" SET "Name" = 'Name B' WHERE "Id" = '...';
```

User A's change is lost, and nobody is notified.

## Optimistic Concurrency (API 3+)

Optimistic concurrency adds a **version check** to the `UPDATE` statement:

```sql
-- User A saves first (xmin was 100)
UPDATE "RetroBoards" SET "Name" = 'New' WHERE "Id" = '...' AND xmin = 100;
-- Rows affected: 1 ✅  (xmin becomes 101)

-- User B tries to save (but xmin is now 101, not 100)
UPDATE "RetroBoards" SET "Name" = 'Other' WHERE "Id" = '...' AND xmin = 100;
-- Rows affected: 0 ❌  → DbUpdateConcurrencyException!
```

### PostgreSQL's xmin Column

PostgreSQL has a built-in system column called `xmin` that changes every
time a row is updated. EF Core's Npgsql provider can use this as a
concurrency token:

```csharp
// EF Core configuration
public class RetroBoardConfiguration : IEntityTypeConfiguration<RetroBoard>
{
    public void Configure(EntityTypeBuilder<RetroBoard> builder)
    {
        builder.UseXminAsConcurrencyToken();
    }
}
```

This is elegant because:
- No extra column in the database
- No extra property on the entity (beyond a `uint Version` for mapping)
- PostgreSQL manages it automatically

### Entity Mapping

```csharp
public class RetroBoard : AuditableEntityBase, IAggregateRoot
{
    // Maps to PostgreSQL's xmin system column
    public uint Version { get; private set; }

    // ... rest of the aggregate
}
```

## Handling Conflicts

When EF Core throws `DbUpdateConcurrencyException`, the API returns
**409 Conflict** with a Problem Details response:

```csharp
// Global exception handling middleware
catch (DbUpdateConcurrencyException)
{
    context.Response.StatusCode = 409;
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = 409,
        Title = "Concurrency conflict",
        Detail = "The resource was modified by another request. Please retry."
    });
}
```

The client can then retry the operation (reload + reapply + save).

## Concurrency Is Not Consistency

Concurrency safety and consistency boundaries are **related but distinct**
concepts. They are easy to conflate because in aggregate-based designs
(API 3–5) a single mechanism — the xmin token on the aggregate root — provides
both. But Api0b demonstrates that you can have one without the other.

**Concurrency safety** answers: *"Can two simultaneous writes corrupt data?"*
It prevents duplicate rows and lost updates. The mechanisms are technical:
concurrency tokens, unique constraints, and exception handling.

**Consistency boundaries** answer: *"Are business invariants guaranteed to hold
across related entities after every transaction?"* They prevent orphaned
children and violated cross-entity rules. The mechanism is architectural: the
aggregate groups entities under a single transaction boundary.

### Api0b: concurrency safety without consistency boundaries

Api0b adds xmin tokens, unique indexes, and two middleware catch blocks. This
is enough to pass the concurrency tests (no duplicates, no silent overwrites).
But it fails the consistency boundary tests:

```
Request A: "Delete column"              Request B: "Add note to column"
    │                                      │
    ├─ Load column (xmin = 50)             ├─ Load column (xmin = 50)
    ├─ Soft-delete column                  ├─ Create note on column
    ├─ SaveChanges ✅                      ├─ SaveChanges ✅ ← no conflict!
    │  (column's xmin changes)             │  (note is a different row)
    │                                      │
    └─ Column deleted                      └─ Note orphaned on deleted column
```

The xmin token on the column row only protects **that row**. The note is a
separate row with its own (or no) xmin token. There is no shared version that
spans the column and its notes.

In API 3, both operations go through the RetroBoard aggregate, which has a
single xmin token. Deleting a column bumps the aggregate's xmin. Adding a note
also goes through the aggregate, sees the xmin mismatch, and fails with a 409.

### Summary

| | Prevents duplicates | Prevents lost updates | Prevents cross-entity violations |
|---|---|---|---|
| **Api0b** (DB mechanisms only) | ✅ unique constraints | ✅ xmin per entity | ❌ no shared boundary |
| **API 3+** (aggregates) | ✅ invariant check + xmin | ✅ xmin on aggregate root | ✅ single xmin spans all children |

See [Consistency Boundaries](consistency-boundaries.md) for the full
architectural explanation of why aggregates provide both guarantees.

## Concurrency at Each API Tier

| Tier | Strategy | Concurrent Duplicate Column | Concurrent Duplicate Vote |
|------|----------|---------------------------|--------------------------|
| Api0a | None | ❌ Both succeed (duplicate!) | ❌ Both succeed |
| Api0b | DB constraints + xmin | ✅ DB unique constraint → 409 | ✅ DB unique constraint → 409 |
| API 1 | None | ❌ Both succeed (duplicate!) | ❌ Both succeed |
| API 2 | None | ❌ Both succeed (duplicate!) | ❌ Both succeed |
| API 3 | Optimistic (xmin on RetroBoard) | ✅ One fails with 409 | ✅ One fails with 409 |
| API 4 | Optimistic (xmin on RetroBoard + Vote) | ✅ One fails with 409 | ✅ DB unique constraint catches it |
| API 5 | Same as API 4 | ✅ One fails with 409 | ✅ DB unique constraint catches it |

> **Note:** Api0b and API 3+ both pass the concurrency tests, but they achieve
> this differently. Api0b relies entirely on **database mechanisms** (unique
> indexes + xmin + middleware catch blocks). API 3+ uses **aggregate boundaries**
> with optimistic concurrency tokens on the aggregate root, which also provides
> consistency guarantees for cross-entity rules that Api0b does not have.
> See [API 0 — Transaction Script](../migration/api0-transaction-script.md)
> for the full comparison.

## Where to Go Next

- [Consistency Boundaries](consistency-boundaries.md) — The architectural
  context that makes concurrency tokens meaningful.
- [Aggregates](aggregates.md) — Why concurrency tokens go on aggregate roots.
