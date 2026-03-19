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
operations go through the RetroBoard aggregate, which loads the entire graph.
The soft-deleted column is excluded by EF Core's query filter during hydration,
so the aggregate root cannot find it — the add-note operation is rejected
because the column doesn't exist within the aggregate's boundary.

### Why the distinction matters

| Concern | What it prevents | Mechanism | Api0b | API 3+ |
|---------|-----------------|-----------|-------|--------|
| **Concurrency safety** | Duplicate data, lost updates | xmin tokens, unique constraints, catch blocks | ✅ | ✅ |
| **Consistency boundary** | Orphaned children, violated cross-entity invariants | Aggregate root that loads + validates the full entity graph | ❌ | ✅ |

In aggregate-based designs (API 3–5), consistency comes from the aggregate
loading and validating the **complete state** of its children — not solely from
the xmin token. The xmin token on the root protects against concurrent updates
to the root row itself (e.g., renaming the retro), while the aggregate's
in-memory invariant checks and the database unique constraints work together
to prevent duplicate children. The consistency boundary is primarily an
**architectural guarantee** (all operations go through the root's methods,
which see the full graph), with xmin and unique constraints as complementary
technical mechanisms.

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

In API 3, the RetroBoard aggregate is the consistency boundary. Every mutating
method on the aggregate root calls `BumpVersion()`, which touches
`LastUpdatedAt` to force EF Core to generate an `UPDATE` on the root row.
This means the xmin concurrency token is checked on **every** write — including
child INSERTs like adding a column:

```
Request A: "Add column 'Feedback'"     Request B: "Add column 'Feedback'"
    │                                      │
    ├─ Load aggregate (xmin = 100)         ├─ Load aggregate (xmin = 100)
    ├─ AddColumn() → OK (in-memory)        ├─ AddColumn() → OK (in-memory)
    ├─ BumpVersion() → LastUpdatedAt       ├─ BumpVersion() → LastUpdatedAt
    ├─ SaveChanges ✅                      │
    │  → INSERT Column + UPDATE RetroBoard  ├─ SaveChanges (xmin ≠ 100!) ❌
    │    (xmin 100 → 101)                  │  → DbUpdateConcurrencyException
    └─ Success                             └─ 409 Conflict
```

This is the [Vaughn Vernon approach](https://vaughnvernon.co/): every command
that modifies the aggregate must advance its version, so that concurrent
mutations always contend on the root's xmin. The DB unique constraint on
`(RetroBoardId, Name)` remains as a defence-in-depth safety net, but it is no
longer the primary concurrency mechanism.

### Why BumpVersion matters

Without `BumpVersion()`, adding a child entity only generates an `INSERT` on
the child's table — the root row is untouched, and xmin is never checked.
The naïve aggregate would silently fall back to the DB unique constraint for
concurrent duplicate prevention — exactly the same mechanism Api0b uses.
This "works by accident" for **uniqueness invariants** (which map to DB
constraints), but fails for invariants that **cannot** be expressed as
constraints:

```csharp
// A count-based invariant — no DB constraint can enforce this:
if (column.Notes.Count >= MaxNotesPerColumn)
    throw new InvariantViolationException("Column is full.");
```

Two concurrent requests could both see 9/10 notes, both pass the check, and
both INSERT — ending up with 11. `BumpVersion()` prevents this because the
second request's `SaveChanges` detects the xmin mismatch before the INSERT
reaches the database.

> [!TIP]
> **Rule of thumb:** if your aggregate has any invariant that isn't backed by a
> database unique constraint, you need `BumpVersion()` (or an equivalent
> mechanism) to make the xmin token fire on every mutation. Even for uniqueness
> invariants, `BumpVersion()` provides a stronger guarantee — the conflict is
> detected at the aggregate level rather than the database level, producing
> a cleaner `DbUpdateConcurrencyException` instead of a `DbUpdateException`.

### The trade-off

`BumpVersion()` means **every write to the aggregate generates an extra
`UPDATE`** on the root row. For a large aggregate like API 3's RetroBoard
(which contains all columns, notes, AND votes), this means even two users
voting on *different* notes in the same retro will conflict — the classic
"aggregate explosion" problem. API 4 addresses this by extracting Vote.

Where the aggregate boundary **also** shines is in **cross-entity validation**.
Because all operations go through the root, the root has access to the full
graph. When you call `retro.AddNote(columnId, text)`, the aggregate root can
verify the column exists, isn't deleted, and the note text is unique within it —
all in memory, in a single method call. Without the aggregate, the endpoint
handler would have to assemble these checks itself (and might forget some, as
API 1/2 demonstrate with the "vote by non-member" gap).

## Right-Sizing Aggregates (API 4)

API 3's RetroBoard aggregate is too large. With `BumpVersion()`, **every**
write to the aggregate — including voting — bumps the root's xmin. Two users
voting on different notes in the same retro conflict with each other.

API 4 extracts Vote as its own aggregate:

| Operation | API 3 Aggregate | API 4 Aggregate |
|-----------|----------------|-----------------|
| Add column | RetroBoard (large) | RetroBoard (smaller) |
| Add note | RetroBoard (large) | RetroBoard (smaller) |
| Cast vote | RetroBoard (large) ⚠️ | **Vote** (tiny) ✅ |

Now voting doesn't lock the retro board, and two users can vote concurrently
on different notes without conflict.

## The Trade-Off: BumpVersion vs. Split Aggregates

Smaller aggregates mean:
- ✅ Less write contention (voting doesn't bump the RetroBoard's xmin)
- ✅ Smaller memory footprint per operation
- ✅ Better scalability

But also:
- ❌ Cross-aggregate invariants can't be enforced via `BumpVersion()`
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

This works because "one vote per user per note" maps to a DB unique constraint.
But API 5's `BudgetVotingStrategy` has a **count-based invariant**
(`MaxVotesPerColumn`) that cannot be expressed as a unique constraint. Two
concurrent vote requests could both see 2/3 budget used, both pass the check,
and both INSERT — resulting in 4/3. In API 3, `BumpVersion()` would prevent
this because both requests would contend on the RetroBoard's xmin. In API 4/5
(with Vote as its own aggregate), there is no shared root to bump — this is
the explicit trade-off of splitting the aggregate.

> [!NOTE]
> This is not a bug — it's a **documented design trade-off**. The budget
> invariant violation is unlikely in practice (it requires two requests within
> the same database transaction window) and the consequence is minor (one extra
> vote). In a high-stakes domain, you might use a serializable transaction or
> advisory lock for the budget check. In this educational repository, we
> document the gap to illustrate that aggregate boundaries are about choosing
> which invariants get strong consistency.

## Where to Go Next

- [Effective Aggregate Design](https://www.dddcommunity.org/library/vernon_2011/)
  by Vaughn Vernon — the three-part series that defines the rules for aggregate
  sizing and concurrency. Part II covers why the aggregate root's version must
  advance on every mutation (the `BumpVersion()` pattern used in API 3).
- [Concurrency Control](concurrency.md) — The mechanism that makes consistency
  boundaries work.
- [Aggregates](aggregates.md) — Revisit aggregate design with this context.
