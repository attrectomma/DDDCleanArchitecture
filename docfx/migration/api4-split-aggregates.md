# API 4 — Split Aggregates

> **Pattern:** Three aggregates (Project, RetroBoard, Vote). Vote is extracted
> from RetroBoard to reduce write contention. Cross-aggregate checks use DB
> constraints as safety nets.

## What Changes from API 3

| Aspect | API 3 | API 4 |
|--------|-------|-------|
| Aggregates | 3 (User, Project, RetroBoard) | **4** (+ Vote) |
| RetroBoard contains | Columns → Notes → **Votes** | Columns → Notes (no votes) |
| Voting locks | Entire retro ⚠️ | **Only the Vote row** ✅ |
| Vote invariant | Aggregate root method | **App check + DB unique constraint** |

## Why Extract Vote?

Voting is the **most frequent write operation** in a retro. In API 3, every
vote locks the entire RetroBoard aggregate (via xmin), meaning:

- Two users voting on **different notes** in the same retro → conflict ❌
- Loading a retro for a vote pulls 200+ entities → slow

Extracting Vote as its own aggregate means:

- Vote is a tiny aggregate (one row) → fast to load and save
- Two concurrent votes on different notes → **no conflict** ✅
- RetroBoard aggregate shrinks → faster for column/note operations

## The Cross-Aggregate Trade-off

The "one vote per user per note" invariant can no longer be enforced inside
an aggregate. Instead:

```csharp
// VoteService — Application-level check (best effort)
if (await _voteRepository.ExistsAsync(noteId, request.UserId, ct))
    throw new InvariantViolationException("Already voted.");

// Database — Unique constraint (ultimate safety net)
builder.HasIndex(v => new { v.NoteId, v.UserId })
    .IsUnique();
```

If two concurrent vote requests both pass the application check, the DB
constraint catches the second one:

```csharp
catch (DbUpdateException ex)
    when (ex.InnerException is PostgresException { SqlState: "23505" })
{
    throw new InvariantViolationException("Already voted.");
}
```

## Concurrency Test Results

```
✅ All tests pass (same as API 3)
✅ Concurrent votes on different notes — no conflict!
```

## What's Still Not Great

- **Full aggregate loading for reads** — GET requests still load the entire
  RetroBoard aggregate, even though they only need a read-only view.
- **No read/write separation** — Same code path for reading and writing.
- **Service organization** — Services are organized by aggregate (noun-centric),
  not by use case (behavior-centric).

## What Changes in API 5

→ [API 5 — CQRS + MediatR](api5-cqrs-mediatr.md): Introduces Command/Query
Responsibility Segregation. Reads bypass aggregates entirely. Writes go through
command handlers. Cross-cutting concerns are handled by pipeline behaviors.
