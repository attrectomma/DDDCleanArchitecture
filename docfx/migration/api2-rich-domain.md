# API 2 — Rich Domain Models

> **Pattern:** Same layers as API 1, but entities own their business logic.

## What Changes from API 1

| Aspect | API 1 | API 2 |
|--------|-------|-------|
| Entity setters | Public | **Private** |
| Entity constructors | Default | **Parameterized** (required fields) |
| Collection exposure | `ICollection<T>` | **`IReadOnlyCollection<T>`** |
| Within-entity rules | In services | **In entity methods** |
| Cross-entity rules | In services | **Still in services** |

## The Key Refactoring

Business logic that involves only one entity moves into that entity:

```csharp
// API 2 — Note enforces the one-vote-per-user rule itself
public class Note : AuditableEntityBase
{
    private readonly List<Vote> _votes = new();

    public Vote CastVote(Guid userId)
    {
        if (_votes.Any(v => v.UserId == userId))
            throw new InvariantViolationException(
                $"User {userId} has already voted on this note.");

        var vote = new Vote(Id, userId);
        _votes.Add(vote);
        return vote;
    }
}
```

The service becomes a thin orchestrator:

```csharp
// API 2 — Service just loads, delegates, and saves
var note = await _noteRepository.GetByIdWithVotesAsync(noteId, ct);
var vote = note.CastVote(request.UserId);  // Entity enforces the rule
await _unitOfWork.SaveChangesAsync(ct);
```

## What's Still Wrong

- **Cross-entity rules** (unique column names across a retro) still live in
  services because entities don't know about their siblings.
- **Loading strategy coupling** — `note.CastVote()` requires the Votes
  collection to be loaded. If the repository forgets `.Include(n => n.Votes)`,
  the check silently passes.
- **No concurrency control** — still no optimistic locking.
- **No consistency boundary** — still no aggregate design.

## Concurrency Test Results

Same as API 1:
```
❌ Concurrent Duplicate — FAIL
❌ Concurrent Vote      — FAIL
```

## What Changes in API 3

→ [API 3 — Aggregates](api3-aggregates.md): Introduces aggregate design.
The RetroBoard becomes an aggregate root that owns columns, notes, and votes.
Cross-entity invariants move into the aggregate root. Optimistic concurrency
is enabled.
