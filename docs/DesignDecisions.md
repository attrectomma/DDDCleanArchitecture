# Design Decisions — Cross-API Comparison

> This document explains the **key architectural decisions** made across all
> five API tiers, why each decision was made, and the trade-offs involved.
> Read this alongside the per-API detailed plans (`01-Api1-DetailedPlan.md`
> through `05-Api5-DetailedPlan.md`) for full context.

---

## 1. Entity Design: Anemic vs. Rich

### Decision

| Tier | Entity Style | Rationale |
|------|-------------|-----------|
| API 1 | **Anemic** — public setters, no methods | Baseline. Shows how most junior codebases start. Entities are property bags shaped by the database. |
| API 2 | **Rich** — private setters, guard constructors, behavior methods | Demonstrates encapsulation. Rules that involve a single entity's data move into the entity itself. |
| API 3–5 | **Rich + Aggregate awareness** | Same as API 2, but entities participate in an aggregate hierarchy. Internal entities (Column, Note) have `internal` constructors so only the aggregate root can create them. |

### Trade-offs

- **Anemic** is easy to understand but scatters business logic across services.
- **Rich** centralizes per-entity logic but cannot enforce cross-entity rules
  (e.g., "column names must be unique within a retro").
- **Aggregate-aware** solves cross-entity rules but increases aggregate
  complexity and coupling between parent and child entities.

### Why Not Start Rich?

API 1 is intentionally "wrong" — it represents the reality of most codebases.
Starting here makes the improvements in later tiers tangible. Students can
diff the service layer between API 1 and API 2 to see exactly what moved.

---

## 2. Business Logic Location

### Decision

| Tier | Within-Entity Rules | Cross-Entity Rules | Cross-Aggregate Rules |
|------|--------------------|--------------------|----------------------|
| API 1 | Service layer | Service layer | Service layer |
| API 2 | **Entity methods** | Service layer | Service layer |
| API 3 | Entity methods | **Aggregate root** | Service layer |
| API 4 | Entity methods | Aggregate root | **Service + DB constraints** |
| API 5 | Entity methods | Aggregate root | **Command handler + DB constraints** |

### Rationale

The progression follows a principle: **put the rule as close to the data it
protects as possible.**

- A rule about a single entity's data → entity method.
- A rule about sibling entities within an aggregate → aggregate root.
- A rule spanning aggregates → application layer + database constraint as safety net.

### Why DB Constraints in API 4?

When Vote is extracted as its own aggregate, the "one vote per user per note"
rule can no longer be checked atomically inside an aggregate. The application
layer checks first (fast, user-friendly error message), and a unique index on
`(NoteId, UserId)` acts as the ultimate safety net for concurrent requests.

---

## 3. Repository Granularity

### Decision

| Tier | Repositories | Pattern |
|------|-------------|---------|
| API 1 | 7 (one per entity) | `IUserRepository`, `IProjectRepository`, `IRetroBoardRepository`, `IColumnRepository`, `INoteRepository`, `IVoteRepository`, `IProjectMemberRepository` |
| API 2 | 7 (same as API 1) | Same — entities are richer but repository structure unchanged |
| API 3 | 3 (one per aggregate) | `IUserRepository`, `IProjectRepository`, `IRetroBoardRepository` |
| API 4 | 4 (one per aggregate) | Same as API 3 + `IVoteRepository` |
| API 5 | 4 (writes only) | Same as API 4, but query handlers bypass repos and use `DbContext` directly |

### Rationale

- **Per-entity repos** (API 1–2) are the intuitive starting point. But they
  allow callers to modify child entities independently of their parent,
  breaking consistency.
- **Per-aggregate repos** (API 3+) enforce that all access goes through the
  aggregate root. There is no `IColumnRepository` — you must load the
  `RetroBoard` to interact with columns.
- **Write-only repos** (API 5) acknowledge that reads and writes have
  different needs. Queries don't need aggregate loading — they project
  directly from the database.

### Trade-off

Fewer repositories means fewer classes to maintain but also means the
aggregate repository must handle more complex queries (e.g., loading the
full aggregate graph with all Includes).

---

## 4. Concurrency Control

### Decision

| Tier | Strategy | Mechanism |
|------|----------|-----------|
| API 1 | None (last write wins) | No concurrency token |
| API 2 | None (last write wins) | No concurrency token |
| API 3 | Optimistic concurrency | PostgreSQL `xmin` on aggregate roots |
| API 4 | Optimistic concurrency | `xmin` on each aggregate root |
| API 5 | Optimistic concurrency | Same as API 4 |

### Why Not API 1 & 2?

Without aggregate boundaries, there is no natural "version" to check. Each
entity is saved independently — there's no single row whose version
represents the consistency state.

### Why `xmin` Instead of a Version Column?

PostgreSQL's `xmin` system column changes on every row update. Using it as
a concurrency token means:

- No extra column in the schema.
- No extra property on the entity (just a `uint Version` mapping).
- PostgreSQL manages it automatically — no trigger or application code needed.
- EF Core's Npgsql provider supports it natively via `UseXminAsConcurrencyToken()`.

### Why Optimistic, Not Pessimistic?

Pessimistic locking (e.g., `SELECT ... FOR UPDATE`) holds database locks for
the duration of the operation. In a web API with unpredictable response times,
this leads to lock contention and potential deadlocks. Optimistic locking
detects conflicts at save time and lets the caller retry — a better fit for
HTTP APIs.

---

## 5. Aggregate Sizing

### Decision

| Tier | RetroBoard Aggregate Contains | Vote Ownership |
|------|------------------------------|---------------|
| API 3 | Columns → Notes → **Votes** | Inside RetroBoard |
| API 4 | Columns → Notes (no votes) | **Separate Vote aggregate** |
| API 5 | Same as API 4 | Same as API 4 |

### The Problem with API 3

The RetroBoard aggregate in API 3 is too large. A retro with 5 columns, 50
notes, and 200 votes loads **255 entities** for every operation — even a
simple vote cast. Worse: any write to the aggregate increments `xmin`, so two
users voting on different notes in the same retro conflict with each other.

### The Fix in API 4

Extracting Vote as its own aggregate:

- **Reduces loading cost** — voting loads one row, not 255.
- **Eliminates false conflicts** — votes on different notes are independent.
- **Keeps RetroBoard focused** — the retro aggregate manages structure
  (columns + notes), not usage (votes).

### The Trade-off

The "one vote per user per note" rule crosses aggregates. It cannot be
enforced atomically by a single aggregate's transaction. The solution:
application-level check + database unique constraint.

---

## 6. Service vs. Handler Organization

### Decision

| Tier | Application Layer | Organization |
|------|------------------|-------------|
| API 1–4 | Services | Noun-centric: `ColumnService`, `NoteService`, `VoteService` |
| API 5 | MediatR handlers | Behavior-centric: `AddColumnCommand`, `CastVoteCommand`, `GetRetroBoardQuery` |

### Why Services First?

Services are the natural starting point. Developers group related operations
by the entity they affect. This works until:

- Services grow large (one service handles CRUD + invariants + mapping).
- Cross-cutting concerns (logging, validation, transactions) are duplicated
  in every service method.
- Read and write paths have different performance characteristics but share
  the same code.

### Why Handlers in API 5?

Each handler is a small, focused class that does one thing:

- **Single Responsibility** — `AddColumnCommandHandler` only knows how to
  add a column. It doesn't also handle column deletion, renaming, or listing.
- **Open/Closed Principle** — Adding a new use case means adding a new
  handler, not modifying an existing service.
- **Pipeline behaviors** — Cross-cutting concerns (validation, logging,
  transactions) are applied automatically via MediatR's pipeline, not
  duplicated in every method.

### Trade-off

API 5 has ~50 files in the Application layer versus ~10 in API 1. More files
means more navigation, more indirection, and a steeper learning curve. This
is worthwhile when the team is large and features are added frequently, but
it's over-engineering for a small CRUD app.

---

## 7. Read/Write Separation (CQRS)

### Decision

| Tier | Read Path | Write Path |
|------|-----------|------------|
| API 1–4 | Load aggregate via repository, map to DTO | Load aggregate via repository, invoke domain method, save |
| API 5 | **Project from `DbContext` with `AsNoTracking()`** | Load aggregate via repository, invoke domain method, save |

### Why CQRS in API 5?

Reads and writes have fundamentally different needs:

| Need | Write | Read |
|------|-------|------|
| Full aggregate graph | ✅ For invariant checks | ❌ Over-fetching |
| Change tracking | ✅ EF needs to detect changes | ❌ Wasted overhead |
| Concurrency token | ✅ Optimistic locking | ❌ Reads don't conflict |

CQRS acknowledges this asymmetry. Query handlers in API 5 project directly
from the database, skipping aggregate loading, change tracking, and Include
chains.

### Why Not Full CQRS?

API 5 uses "CQRS lite" — same database, separated code paths. A full CQRS
implementation would have a separate read database (e.g., a denormalized
projection or read replica). That adds eventual consistency complexity that
is beyond the scope of this educational repository.

---

## 8. Soft Delete

### Decision

All tiers use the same soft-delete strategy: an EF Core `SaveChangesInterceptor`
converts `EntityState.Deleted` to `EntityState.Modified` with `DeletedAt`
set to `DateTime.UtcNow`. A global query filter (`HasQueryFilter`) excludes
soft-deleted entities from all queries.

### Why Interceptor, Not Entity Method?

An entity method like `entity.SoftDelete()` would require every service/handler
to remember to call it instead of `repository.Delete()`. The interceptor
approach is transparent — callers use the normal EF Core delete flow, and the
interceptor silently converts it.

### Why Global Query Filter?

Without a global filter, every query would need `.Where(e => e.DeletedAt == null)`.
One forgotten filter means deleted data leaks into results. Global query
filters make the correct behavior the default.

---

## 9. Shared Integration Test Suite

### Decision

All five APIs are verified by the **same set of integration tests**. Shared
abstract base classes in `RetroBoard.IntegrationTests.Shared` define the
test logic, and API-specific test projects inherit them.

### Why Shared Tests?

- **Identical contract** — All APIs expose the same REST surface. A test
  that works against API 1 should work against API 5.
- **Demonstrates the value** — The concurrency tests intentionally fail on
  API 1 and 2, making the improvement in API 3+ tangible.
- **Maintainability** — One set of test logic instead of five copies.

### Why Some Tests Intentionally Fail?

The concurrency tests (`ConcurrencyTestsBase`) are designed to demonstrate
that check-then-act race conditions exist in API 1 and 2. Students run the
tests and see:

```
API 1: ❌ Concurrent Duplicate — FAIL (both requests succeed)
API 3: ✅ Concurrent Duplicate — PASS (second request gets 409)
```

This makes the abstract concept of "consistency boundary" concrete.

---

## 10. Technology Choices

### PostgreSQL (Not SQL Server)

- `xmin` concurrency token is a PostgreSQL-native feature — elegant and zero-config.
- Testcontainers has excellent PostgreSQL support with lightweight Alpine images.
- PostgreSQL is free and cross-platform, reducing barriers for students.

### FluentValidation (Not Data Annotations)

- Separates validation logic from DTO definitions.
- Supports complex cross-property validation.
- Integrates with MediatR pipeline behaviors in API 5.

### MediatR (Not Custom Mediator)

- Industry-standard library for .NET CQRS implementations.
- Rich pipeline behavior support for cross-cutting concerns.
- Extensive documentation and community familiarity.

### No AutoMapper

- Manual mapping keeps transformations explicit and educational.
- Students see exactly how entities map to DTOs — no "magic".
- For a teaching repository, transparency trumps convenience.

---

## 11. What This Repository Intentionally Does NOT Cover

| Topic | Why Excluded |
|-------|-------------|
| Authentication / Authorization | Orthogonal to architecture evolution; adds noise |
| Read replicas / Event Sourcing | Beyond "CQRS lite" scope; would require a 6th API |
| Microservices | All APIs are monoliths; distributed architecture is a separate topic |
| Background jobs | No async processing needed for the retro board domain |
| Caching | Would obscure the data access patterns being taught |
| API versioning | All APIs share one version; versioning is a separate concern |

These are all important topics, but including them would dilute the core
teaching goal: **showing how domain modeling and architecture evolve together.**

---

## Summary: When to Use Which Tier

| Scenario | Recommended Tier |
|----------|-----------------|
| Prototype / hackathon / tiny CRUD app | API 1 (Anemic) — speed over structure |
| Small team, simple domain, no concurrency concerns | API 2 (Rich Domain) — better encapsulation |
| Medium domain, multiple users writing concurrently | API 3 (Aggregates) — consistency boundaries |
| High write contention on specific entities | API 4 (Split Aggregates) — right-sized aggregates |
| Large team, complex domain, read-heavy workload | API 5 (CQRS + MediatR) — separated concerns |

> **The best architecture is the simplest one that solves your actual problems.**
> Don't use API 5 patterns for an API 1 problem.
