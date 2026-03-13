# Aggregates & Aggregate Roots

## What Is an Aggregate?

An **aggregate** is a cluster of domain objects that are treated as a single
unit for the purpose of data changes. Every aggregate has a **root entity**
(the aggregate root) through which all access to the aggregate must pass.

> An aggregate defines a **consistency boundary** — all invariants within the
> aggregate are guaranteed to be consistent at the end of every transaction.

## The RetroBoard Example

### API 3: Two Aggregates

```
┌─────────────────────────────────────────────┐
│  RetroBoard Aggregate                       │
│  ┌───────────────┐                          │
│  │  RetroBoard   │ ← Aggregate Root         │
│  │  (Id, Name)   │                          │
│  └───────┬───────┘                          │
│          │ owns                              │
│  ┌───────▼───────┐                          │
│  │   Column      │                          │
│  │  (Id, Name)   │                          │
│  └───────┬───────┘                          │
│          │ owns                              │
│  ┌───────▼───────┐                          │
│  │    Note       │                          │
│  │  (Id, Text)   │                          │
│  └───────┬───────┘                          │
│          │ owns                              │
│  ┌───────▼───────┐                          │
│  │    Vote       │                          │
│  │ (NoteId,      │                          │
│  │  UserId)      │                          │
│  └───────────────┘                          │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│  Project Aggregate                          │
│  ┌───────────────┐                          │
│  │   Project     │ ← Aggregate Root         │
│  │  (Id, Name)   │                          │
│  └───────┬───────┘                          │
│          │ owns                              │
│  ┌───────▼───────┐                          │
│  │ ProjectMember │                          │
│  │(ProjectId,    │                          │
│  │ UserId)       │                          │
│  └───────────────┘                          │
└─────────────────────────────────────────────┘
```

### API 4: Three Aggregates (Vote Extracted)

```
┌──────────────────────┐  ┌──────────────┐
│  RetroBoard Agg.     │  │  Vote Agg.   │
│  RetroBoard (root)   │  │  Vote (root) │
│    └─ Column         │  │  (NoteId,    │
│        └─ Note       │  │   UserId)    │
│           (no votes) │  └──────────────┘
└──────────────────────┘
```

## Aggregate Rules

1. **External references use IDs only.** Outside code can reference an
   aggregate by the root's ID, but cannot hold a direct reference to an
   internal entity.

2. **One repository per aggregate.** There is no `IColumnRepository` in API 3
   — columns are accessed through `IRetroBoardRepository`.

3. **One transaction per aggregate.** A single `SaveChangesAsync` call should
   modify at most one aggregate. Cross-aggregate changes require separate
   transactions.

4. **All mutations go through the root.** You don't call `column.AddNote()`
   directly — you call `retroBoard.AddNote(columnId, text)`.

## Why Aggregate Roots?

The aggregate root is the **gatekeeper**. It can enforce invariants that span
multiple entities within the aggregate:

```csharp
// RetroBoard (aggregate root) enforces cross-entity invariant
public Column AddColumn(string name)
{
    // This invariant spans the entire Columns collection
    if (_columns.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        throw new InvariantViolationException(
            $"Column name '{name}' already exists in retro '{Name}'.");

    var column = new Column(Id, name);
    _columns.Add(column);
    return column;
}
```

In API 2, this check couldn't live inside `Column` because a Column doesn't
know about its siblings. The aggregate root does.

## The Aggregate Explosion Problem

In API 3, the RetroBoard aggregate contains **everything** — columns, notes,
and votes. For a retro with 5 columns, 50 notes, and 200 votes, every
operation loads 255+ entities.

Worse: any write to the aggregate (even voting on a single note) locks the
entire aggregate via optimistic concurrency, causing **write contention**.

This is the aggregate explosion problem, and it's why API 4 extracts Vote
as its own aggregate. See [Consistency Boundaries](consistency-boundaries.md).

## Where to Go Next

- [Consistency Boundaries](consistency-boundaries.md) — The theory behind
  aggregate sizing.
- [Concurrency Control](concurrency.md) — How aggregates enable optimistic
  locking.
