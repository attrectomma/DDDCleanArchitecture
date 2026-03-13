# Soft Delete

## What Is Soft Delete?

Instead of permanently removing a row from the database (`DELETE FROM ...`),
**soft delete** marks the row as deleted by setting a `DeletedAt` timestamp.
The row remains in the database but is excluded from all normal queries.

## Implementation in RetroBoard

### 1. Base Entity Property

All entities inherit `DeletedAt` from `AuditableEntityBase`:

```csharp
public abstract class AuditableEntityBase
{
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}
```

### 2. EF Core Interceptor

The `AuditInterceptor` converts `EntityState.Deleted` into a soft delete:

```csharp
case EntityState.Deleted:
    entry.State = EntityState.Modified;       // Don't actually delete
    entry.Entity.DeletedAt = DateTime.UtcNow; // Just mark as deleted
    break;
```

This means calling `_context.Remove(entity)` or `repository.Delete(entity)`
triggers a soft delete automatically.

### 3. Global Query Filter

EF Core's global query filter ensures soft-deleted entities are never returned
by normal queries:

```csharp
builder.HasQueryFilter(e => e.DeletedAt == null);
```

This is applied to **every entity** in every API.

### 4. Unique Index Consideration

With soft delete, unique indexes must account for deleted records. A column
name "Feedback" that was soft-deleted should not prevent creating a new column
named "Feedback". The unique index must be a **filtered index**:

```csharp
builder.HasIndex(c => new { c.RetroBoardId, c.Name })
    .IsUnique()
    .HasFilter("\"DeletedAt\" IS NULL");  // Only enforce for non-deleted rows
```

## Why Soft Delete?

- **Audit trail** — You can always see what was deleted and when.
- **Recovery** — Accidentally deleted data can be restored.
- **Referential integrity** — Foreign keys to soft-deleted rows don't break.
- **Analytics** — Historical data remains available for reporting.

## Trade-offs

- **Query performance** — Every query includes `WHERE DeletedAt IS NULL`.
- **Storage** — Deleted data remains in the database.
- **Complexity** — Filtered unique indexes, cascade soft deletes.

## Where to Go Next

- [Entities & Anemic Domain Model](entities.md) — The `AuditableEntityBase`
  that provides soft delete.
