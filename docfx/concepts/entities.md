# Entities & the Anemic Domain Model

## What Is an Entity?

An **entity** is an object that has a distinct identity that persists over time.
Two entities are the same if they have the same ID, even if their properties
differ. In the RetroBoard domain, `User`, `Project`, `RetroBoard`, `Column`,
`Note`, and `Vote` are all entities.

In .NET, entities are typically C# classes with properties that map to database
columns.

## What Is an Anemic Domain Model?

An **anemic domain model** is a design where entity classes are pure data
holders — they have properties with public getters and setters, but contain
**no business logic**. All behavior lives elsewhere, typically in a service
layer.

Martin Fowler called this an [anti-pattern](https://martinfowler.com/bliki/AnemicDomainModel.html),
but it's extremely common — especially in codebases written by developers
early in their careers.

## Example: API 1 (Anemic)

In API 1, the `Column` entity is a plain property bag:

```csharp
// API 1 — Anemic entity. No behavior, no invariant enforcement.
// Business logic lives in ColumnService instead.
public class Column : AuditableEntityBase
{
    public Guid RetroBoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RetroBoard RetroBoard { get; set; } = null!;
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
```

Notice:
- **Public setters** — anyone can change any property at any time.
- **No constructor** — no way to enforce required fields at creation time.
- **No methods** — the entity doesn't know its own business rules.

The rule "column names must be unique within a retro board" is enforced in
`ColumnService`:

```csharp
// API 1 — Business rule enforced in the service layer
if (await _columnRepository.ExistsByNameInRetroAsync(retroBoardId, request.Name, ct))
    throw new DuplicateException("Column", "Name", request.Name);
```

## Why Is This a Problem?

1. **Scattered logic** — Business rules live across multiple service classes.
   To understand "what are the rules for columns?" you must read the entire
   `ColumnService`.

2. **No encapsulation** — Any code with a reference to the entity can change
   its state, potentially violating invariants.

3. **Duplication risk** — If two services need the same rule, the check gets
   duplicated (or one forgets to check).

4. **Untestable domain** — You can't test business rules in isolation because
   they're entangled with infrastructure (repositories, database queries).

## The AuditableEntityBase

All entities across all five APIs inherit from a common base class:

```csharp
public abstract class AuditableEntityBase
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}
```

This provides:
- A `Guid` primary key for every entity
- Audit timestamps populated by an EF Core interceptor
- A `DeletedAt` timestamp for [soft delete](soft-delete.md)

## Where to Go Next

- [Rich Domain Models](rich-domain-models.md) — API 2 moves business logic
  into the entities themselves.
- [Services & Orchestration](services.md) — Understand what services are
  *supposed* to do (and what they shouldn't).
