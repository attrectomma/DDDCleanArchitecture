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

## Concurrency Safety Without a Consistency Boundary (Api0b)

Api0b is the odd one out in the comparison tables — it **passes the
concurrency tests** but still **fails the consistency boundary tests**. This
makes it the best example in the codebase for understanding that concurrency
and consistency are **different concerns** that happen to overlap in
aggregate-based designs.

### What Api0b adds

Api0b adds three things over Api0a (and API 1/2):

1. **xmin concurrency tokens** on User, Project, and RetroBoard — detects when
   a row was modified between read and write.
2. **Unique indexes** on Column (Name + RetroBoardId), Note (Text + ColumnId),
   and Vote (NoteId + UserId) — these already exist in Api0a but the middleware
   doesn't catch the resulting `DbUpdateException`.
3. **Two catch blocks in middleware** — one for `DbUpdateConcurrencyException`
   (xmin mismatch → 409) and one for `DbUpdateException` with PostgreSQL error
   code 23505 (unique constraint → 409).

This is enough to prevent **duplicate data** and **silent overwrites**. Two
concurrent requests to create a column with the same name? One succeeds, the
other gets a 409. That's concurrency safety.

### What Api0b still lacks

But Api0b has no **consistency boundary** — no aggregate that groups related
entities into a unit with invariants enforced atomically. Consider this
scenario:

```
Request A: "Delete column 'Feedback'"   Request B: "Add note to 'Feedback'"
    │                                      │
    ├─ Load column → exists                ├─ Load column → exists
    ├─ Soft-delete column                  ├─ Create note on column
    ├─ SaveChanges ✅                      ├─ SaveChanges ✅
    │                                      │
    └─ Column deleted                      └─ Note orphaned on deleted column!
```

No xmin token or unique constraint can prevent this. The column and the note are
saved in **separate transactions** with no shared version check. In API 3, both
operations go through the RetroBoard aggregate, which has a single xmin token —
the second request would see a version mismatch and fail.

### Why the distinction matters

| Concern | What it prevents | Mechanism | Api0b | API 3+ |
|---------|-----------------|-----------|-------|--------|
| **Concurrency safety** | Duplicate data, lost updates | xmin tokens, unique constraints, catch blocks | ✅ | ✅ |
| **Consistency boundary** | Orphaned children, violated cross-entity invariants | Aggregate root with single concurrency token spanning all children | ❌ | ✅ |

In aggregate-based designs (API 3–5), the concurrency token on the aggregate
root serves **both** purposes — it detects concurrent writes (concurrency) and
it ensures all invariants within the aggregate are checked atomically
(consistency). This is why the two concepts are so easy to conflate: when you
have aggregates, you usually get both for free.

Api0b proves they are separable. You can bolt on concurrency safety with
database mechanisms alone (~35 lines of code), but consistency boundaries
require an **architectural concept** — the aggregate — that groups entities
and enforces rules as a unit.

> [!TIP]
> If your domain has few cross-entity invariants (or you're comfortable relying
> on DB constraints as safety nets), Api0b's approach may be sufficient. If
> your domain has invariants that span multiple entities and must be enforced
> atomically, you need aggregates.

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
