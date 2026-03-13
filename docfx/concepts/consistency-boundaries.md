# Consistency Boundaries

## What Is a Consistency Boundary?

A **consistency boundary** defines the scope within which business invariants
are guaranteed to be consistent after every transaction. In DDD, this boundary
is the **aggregate**.

Within a consistency boundary:
- All invariants are checked before saving.
- Changes are atomic — all or nothing.
- Concurrent writes are detected and rejected.

Outside a consistency boundary:
- Data may be **eventually consistent**.
- Cross-boundary checks are "best effort" — confirmed by database constraints.

## No Boundary (API 1 & 2)

In API 1 and 2, there is no consistency boundary. Each entity is loaded and
saved independently:

```
Request A: "Add column 'Feedback'"     Request B: "Add column 'Feedback'"
    │                                      │
    ├─ Check: exists? → No                 ├─ Check: exists? → No
    ├─ Create column                       ├─ Create column
    ├─ SaveChanges ✅                      ├─ SaveChanges ✅
    │                                      │
    └─ DUPLICATE! Both succeeded.          └─ DUPLICATE!
```

This is the classic **check-then-act race condition**. The check and the
action are not atomic.

## Aggregate Boundary (API 3)

In API 3, the RetroBoard aggregate is the consistency boundary. Optimistic
concurrency ensures that the second write detects the conflict:

```
Request A: "Add column 'Feedback'"     Request B: "Add column 'Feedback'"
    │                                      │
    ├─ Load aggregate (xmin = 100)         ├─ Load aggregate (xmin = 100)
    ├─ AddColumn() → OK                    ├─ AddColumn() → OK
    ├─ SaveChanges (xmin still 100) ✅     │
    │  → xmin becomes 101                  ├─ SaveChanges (xmin ≠ 100!) ❌
    │                                      │  → DbUpdateConcurrencyException
    └─ Success                             └─ 409 Conflict
```

The aggregate + optimistic concurrency makes the operation **atomic**.

## Right-Sizing Aggregates (API 4)

API 3's RetroBoard aggregate is too large. Voting — the most frequent write
operation — locks the entire retro. Two users voting on different notes in
the same retro conflict with each other.

API 4 extracts Vote as its own aggregate:

| Operation | API 3 Aggregate | API 4 Aggregate |
|-----------|----------------|-----------------|
| Add column | RetroBoard (large) | RetroBoard (smaller) |
| Add note | RetroBoard (large) | RetroBoard (smaller) |
| Cast vote | RetroBoard (large) ⚠️ | **Vote** (tiny) ✅ |

Now voting doesn't lock the retro board, and two users can vote concurrently
on different notes without conflict.

## The Trade-Off

Smaller aggregates mean:
- ✅ Less write contention
- ✅ Smaller memory footprint per operation
- ✅ Better scalability

But also:
- ❌ Cross-aggregate invariants can't be enforced atomically
- ❌ Need database constraints as safety nets
- ❌ More complex error handling (catching constraint violations)

The "one vote per user per note" invariant in API 4:

```csharp
// Application-level check (best effort)
if (await _voteRepository.ExistsAsync(noteId, userId, ct))
    throw new InvariantViolationException("Already voted.");

// DB unique constraint (ultimate safety net)
builder.HasIndex(v => new { v.NoteId, v.UserId }).IsUnique();
```

## Where to Go Next

- [Concurrency Control](concurrency.md) — The mechanism that makes consistency
  boundaries work.
- [Aggregates](aggregates.md) — Revisit aggregate design with this context.
