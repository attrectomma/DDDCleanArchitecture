# Design Decisions — Cross-API Comparison

> This document explains the **key architectural decisions** made across all
> API tiers, why each decision was made, and the trade-offs involved.
> Read this alongside the per-API detailed plans (`01-Api1-DetailedPlan.md`
> through `06-Api0-TransactionScript-DetailedPlan.md`) for full context.

---

## 1. Entity Design: Anemic vs. Rich

### Decision

| Tier | Entity Style | Rationale |
|------|-------------|-----------|
| API 0 | **Anemic** — public setters, no methods, no layers | Transaction Script pattern. Entities are pure property bags — EF Core mapping targets and nothing more. Same shape as API 1 but in a single project with no Domain layer. |
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
| API 0 | Endpoint handlers | Endpoint handlers | Endpoint handlers |
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
| API 0 | 0 (none — DbContext directly) | Transaction Script — endpoint handlers query and mutate `DbSet<T>` directly. No repository abstraction. |
| API 1 | 7 (one per entity) | `IUserRepository`, `IProjectRepository`, `IRetroBoardRepository`, `IColumnRepository`, `INoteRepository`, `IVoteRepository`, `IProjectMemberRepository` |
| API 2 | 7 (same as API 1) | Same — entities are richer but repository structure unchanged |
| API 3 | 3 (one per aggregate) | `IUserRepository`, `IProjectRepository`, `IRetroBoardRepository` |
| API 4 | 4 (one per aggregate) | Same as API 3 + `IVoteRepository` |
| API 5 | 4 (writes only) | Same as API 4, but query handlers bypass repos and use `DbContext` directly |

### Rationale

- **No repos** (API 0) is the simplest approach — the Transaction Script
  pattern deliberately avoids repository abstractions. Each endpoint handler
  talks to the `DbContext` directly. This is fast to build but provides no
  encapsulation boundary.
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
| API 0a | None (last write wins) | No concurrency token. DB unique indexes exist but exceptions are not caught — concurrent violations return 500. |
| API 0b | Optimistic concurrency (DB-level) | PostgreSQL `xmin` on User, Project, RetroBoard. Middleware catches `DbUpdateConcurrencyException` and `DbUpdateException` (23505). ~35 lines of diff vs. Api0a. |
| API 1 | None (last write wins) | No concurrency token |
| API 2 | None (last write wins) | No concurrency token |
| API 3 | Optimistic concurrency | PostgreSQL `xmin` on aggregate roots |
| API 4 | Optimistic concurrency | `xmin` on each aggregate root |
| API 5 | Optimistic concurrency | Same as API 4 |

### Why Not API 0a, 1 & 2?

Without aggregate boundaries (or xmin tokens in Api0a's case), there is no
natural "version" to check. Each entity is saved independently — there's no
single row whose version represents the consistency state.

Api0b demonstrates that you **can** add concurrency tokens without aggregate
boundaries — by applying `xmin` directly to individual entities and catching
the resulting exceptions in middleware. This is the "database as consistency
boundary" approach. It fixes concurrent duplicate detection but does not
provide the cross-entity consistency guarantees that aggregates (API 3+) offer.

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

### API 5: Conditional Constraints via Options Pattern

API 5 introduces configurable voting strategies (Default vs Budget). The
Budget strategy allows multiple votes per user per note ("dot voting"), so the
unique constraint on `(NoteId, UserId)` cannot be unconditionally applied.

The `RetroBoardDbContext` receives `IOptions<VotingOptions>` and conditionally
applies the unique index in `OnModelCreating`. When configured for Default,
the DB constraint acts as a safety net (same as API 4). When configured for
Budget, the index is non-unique and vote validation relies on application-level
specifications only.

This introduces a new concept: **configuration-dependent database schema** —
managed via a custom `IModelCacheKeyFactory` so EF Core builds separate models
per configuration.

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
  duplicated in every method. The `TransactionBehavior` selectively wraps
  only commands (via `ICommand<T>` marker) in explicit DB transactions;
  queries skip it entirely.

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

All APIs are verified by the **same set of integration tests**. Shared
abstract base classes in `RetroBoard.IntegrationTests.Shared` define the
test logic, and API-specific test projects inherit them.

### Why Shared Tests?

- **Identical contract** — All APIs expose the same REST surface. A test
  that works against API 0a should work against API 5.
- **Demonstrates the value** — The concurrency tests intentionally fail on
  API 0a, 1, and 2, making the improvement in API 0b and API 3+ tangible.
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

## 12. Testing Strategy: Unit Tests + Integration Tests, No Mocking

### Decision

The repository uses **two complementary test layers** — and deliberately
excludes mock-based testing:

| Layer | Scope | Infrastructure | Tiers |
|-------|-------|---------------|-------|
| **Domain unit tests** | Entity/aggregate methods | None — pure in-memory | API 2–5 |
| **Integration tests** | HTTP → Controller → Service/Handler → DB | Testcontainers (PostgreSQL), WebApplicationFactory | API 0–5 |
| ~~Mock-based unit tests~~ | ~~Service/handler orchestration~~ | ~~Moq / NSubstitute~~ | ❌ Not used |

### Why No Mocking?

#### 1. The handlers and services are thin orchestrators

The typical application-layer method in this codebase follows a linear pattern:

```csharp
// API 5 — AddColumnCommandHandler.Handle
RetroBoard retro = await _repository.GetByIdAsync(id, ct)
    ?? throw new NotFoundException("RetroBoard", id);

Column column = retro.AddColumn(request.Name);      // ← Domain logic
await _unitOfWork.SaveChangesAsync(ct);              // ← Persistence
return new ColumnResponse(column.Id, column.Name);   // ← Mapping
```

A mock-based test for this handler would verify: "did you call
`GetByIdAsync`? did you call `SaveChangesAsync`?" — that is **testing
implementation details, not behavior**. If the handler is refactored to use a
different repository method, the mock test breaks even though the observable
behavior is unchanged. This is the classic brittle-mock anti-pattern.

#### 2. The real logic is already unit tested

The invariant check inside `retro.AddColumn(request.Name)` — the part that
can actually fail in interesting ways — is exercised by the domain unit tests.
Mocking the repository to test that `AddColumn` was called does not add
confidence beyond what the domain unit test already provides.

#### 3. Orchestration is covered by integration tests

The integration tests verify the full path: HTTP request → controller →
handler → repository → database → response. They catch:

- Wiring mistakes (wrong DI registration, missing Include, wrong mapping)
- Database behavior (constraint violations, query filter interactions)
- Middleware behavior (exception-to-ProblemDetails mapping)

Mock-based tests would catch **none of these** because they replace the
real collaborators with fakes.

#### 4. Query handlers should not be mock-tested

API 5's query handlers project directly from `DbContext` using LINQ-to-SQL
with `.Select()`, `AsNoTracking()`, and correlated subqueries. Mocking
`DbContext`/`DbSet` is a well-known anti-pattern — the mocked behavior
diverges from real EF Core query translation. The EF Core team themselves
recommend testing queries against a real database.

#### 5. The premise "only API 5 would benefit" does not hold

The logic in `Api5.CastVoteCommandHandler` is **nearly line-for-line
identical** to `Api4.VoteService.CastVoteAsync` — same dependencies
(vote repo, retro repo, project repo, UoW), same cross-aggregate checks,
same orchestration flow. If mocking handlers is worthwhile for API 5, it is
equally worthwhile for API 2–4 services. MediatR dispatch does not make
handler code inherently more "mock-worthy" than service code.

#### 6. Educational risk

In a teaching repository, introducing mocks risks sending the wrong message:
"you should mock everything." The current strategy teaches a cleaner
principle: **design your domain so the interesting logic lives in pure
functions that need no mocks, and verify the wiring with end-to-end tests.**

### When Mocking IS Justified (Outside This Repo)

Mocking becomes valuable when application-layer orchestration has:

- **Significant branching logic** — multiple code paths with different
  outcomes (e.g., saga-style workflows, conditional retry policies).
- **External service calls** — HTTP clients, message queues, file systems
  that are slow, non-deterministic, or unavailable in test environments.
- **Complex coordination** — multi-aggregate transactions where the order
  of operations matters and must be verified.

This repository's handlers do not exhibit these characteristics. Most are
linear load → delegate → save sequences. The two handlers with the most
orchestration logic (`CastVoteCommandHandler` with 3 cross-aggregate checks,
and `NoteRemovedEventHandler` with conditional cleanup) are thoroughly
covered by the integration test suite.

### The Two-Layer Testing Pyramid

```
┌─────────────────────────────────────────┐
│       Integration Tests (API 1–5)       │  ← Verify wiring, DB, HTTP
│   Testcontainers · WebApplicationFactory │
├─────────────────────────────────────────┤
│       Domain Unit Tests (API 2–5)       │  ← Verify invariants, guards,
│   No infra · No mocks · Pure functions  │     domain events, pure logic
└─────────────────────────────────────────┘
```

The unit tests are fast and precise. The integration tests are thorough and
realistic. Together they provide high confidence without the brittleness and
maintenance cost of a mock-heavy middle layer.

---

## Summary: When to Use Which Tier

| Scenario | Recommended Tier |
|----------|-----------------|
| Prototype / hackathon / tiny CRUD app | API 0a (Transaction Script) — maximum speed, minimum ceremony |
| Small team, simple domain, need concurrency safety | API 0b (Transaction Script + DB safety) — simple with protection |
| Small team, simple domain, want layered structure | API 1 (Anemic) — layered but no concurrency |
| Small team, simple domain, no concurrency concerns | API 2 (Rich Domain) — better encapsulation |
| Medium domain, multiple users writing concurrently | API 3 (Aggregates) — consistency boundaries |
| High write contention on specific entities | API 4 (Split Aggregates) — right-sized aggregates |
| Large team, complex domain, read-heavy workload | API 5 (CQRS + MediatR) — separated concerns |

> **The best architecture is the simplest one that solves your actual problems.**
> Don't use API 5 patterns for an API 0 problem.
