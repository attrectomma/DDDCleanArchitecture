# API 0 — Transaction Script (Detailed Implementation Plan)

> **Theme:** Embrace simplicity. A single-project Web API using the
> **Transaction Script** pattern (Fowler, *PoEAA*). No layers, no repositories,
> no services, no Unit of Work abstraction. Endpoints talk directly to the
> `DbContext`. Two versions demonstrate a mini-evolution: **Api0a** has the
> same concurrency failures as API 1/2; **Api0b** fixes them with database
> constraints and error handling — no architectural rewrite required.

---

## Purpose & Pedagogical Goal

The main migration path (API 1 → 5) tells a story of **increasing
sophistication**: richer domains, aggregate boundaries, CQRS, mediator
pipelines. But it never answers the question:

> *"What if I just kept it simple?"*

API 0 answers that question. It is **not** part of the evolutionary
progression. It exists as a **parallel track** — a side-by-side comparison
that demonstrates:

1. **How little code is needed** to implement the same REST contract that
   API 1 spreads across 4 projects, 7 repositories, 7 services, and a UoW.
2. **That concurrency problems can be fixed at the database level** without
   aggregates, rich domain models, or architectural rewrites.
3. **When simplicity is the right choice** — and when it stops being enough.

### The Mini-Evolution

```
Api0a: Transaction Script             "Simple. Same concurrency failures as API 1/2."
  │
  ▼  Add unique indexes + xmin tokens + error handling (~30 lines of diff)
Api0b: Transaction Script + DB         "Same simplicity. Concurrency fixed."
       Concurrency Safety
```

Contrast with the main DDD path:

```
API 1 → API 2 → API 3                 Three full rewrites across 12+ projects
                                       to fix the same two test failures.
```

### Key Teaching Points

| Lesson | How API 0 Teaches It |
|--------|---------------------|
| Architecture should match problem complexity | Same domain, 1 project vs. 4 projects × 5 tiers |
| The database is a valid consistency boundary | Api0b fixes concurrency with DB constraints alone |
| Layering has a cost, not just a benefit | Reader can diff Api0a vs. API 1 — same behavior, fraction of the code |
| Know what you're trading away | Api0 docs explain what you lose (testability, encapsulation, domain expressiveness) |
| YAGNI is a legitimate architectural principle | Not every app needs DDD |

---

## Status Tracking Legend

| Symbol | Meaning |
|--------|---------|
| ⬜ | Not started |
| 🔄 | In progress |
| ✅ | Complete |
| ⏭️ | Skipped (intentionally) |

---

## Phase 1: Api0a — Transaction Script (No Concurrency Safety)

> **Goal:** A single-project Web API with Minimal APIs, anemic entities,
> `DbContext` used directly in endpoint handlers, and FluentValidation.
> Passes CRUD, Invariant, and Soft Delete tests. **Fails** Concurrency and
> Consistency Boundary tests (same as API 1/2).

### 1.1 Project Setup

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.1.1 | Create `src/Api0.TransactionScript/Api0a.WebApi/` folder | ✅ | Single project — no Domain/Application/Infrastructure layers |
| 1.1.2 | Create `Api0a.WebApi.csproj` with NuGet refs (no `Version` attrs — CPM) | ✅ | Refs: `Npgsql.EntityFrameworkCore.PostgreSQL`, `FluentValidation.AspNetCore`, `Swashbuckle.AspNetCore` |
| 1.1.3 | Add project to `RetroBoard.slnx` under `/src/Api0.TransactionScript/` folder | ✅ | |
| 1.1.4 | Verify `dotnet build` succeeds with empty project | ✅ | |

#### Project Structure

```
src/Api0.TransactionScript/
└── Api0a.WebApi/
    ├── Entities/
    │   ├── AuditableEntityBase.cs
    │   ├── User.cs
    │   ├── Project.cs
    │   ├── ProjectMember.cs
    │   ├── RetroBoard.cs
    │   ├── Column.cs
    │   ├── Note.cs
    │   └── Vote.cs
    ├── DTOs/
    │   ├── CreateUserRequest.cs
    │   ├── CreateProjectRequest.cs
    │   ├── AddMemberRequest.cs
    │   ├── CreateRetroBoardRequest.cs
    │   ├── CreateColumnRequest.cs
    │   ├── UpdateColumnRequest.cs
    │   ├── CreateNoteRequest.cs
    │   ├── UpdateNoteRequest.cs
    │   ├── CastVoteRequest.cs
    │   ├── UserResponse.cs
    │   ├── ProjectResponse.cs
    │   ├── ProjectMemberResponse.cs
    │   ├── RetroBoardResponse.cs
    │   ├── ColumnResponse.cs
    │   ├── NoteResponse.cs
    │   └── VoteResponse.cs
    ├── Data/
    │   ├── RetroBoardDbContext.cs
    │   └── Interceptors/
    │       └── AuditInterceptor.cs
    ├── Endpoints/
    │   ├── UserEndpoints.cs
    │   ├── ProjectEndpoints.cs
    │   ├── RetroBoardEndpoints.cs
    │   ├── ColumnEndpoints.cs
    │   ├── NoteEndpoints.cs
    │   └── VoteEndpoints.cs
    ├── Middleware/
    │   └── GlobalExceptionHandlerMiddleware.cs
    ├── Validators/
    │   ├── CreateUserRequestValidator.cs
    │   ├── CreateProjectRequestValidator.cs
    │   ├── CreateColumnRequestValidator.cs
    │   ├── CreateNoteRequestValidator.cs
    │   └── CastVoteRequestValidator.cs
    ├── Program.cs
    ├── appsettings.json
    └── Api0a.WebApi.csproj
```

> **DESIGN:** Notice the absence of Interfaces/, Services/, Repositories/,
> Configurations/ (as separate IEntityTypeConfiguration classes). Entity
> configuration is inline in `OnModelCreating`. This is the Transaction
> Script pattern — each endpoint is a self-contained "script" that loads
> data, applies business logic, and persists changes. All in one place.

### 1.2 Entities (Anemic — Same Shape as API 1)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.2.1 | Create `AuditableEntityBase.cs` | ✅ | Same shape as API 1. Public setters. `Id`, `CreatedAt`, `LastUpdatedAt`, `DeletedAt`, `IsDeleted`. |
| 1.2.2 | Create `User.cs` | ✅ | `Name`, `Email`, navigation to `ProjectMemberships` |
| 1.2.3 | Create `Project.cs` | ✅ | `Name`, navigation to `Members`, `RetroBoards` |
| 1.2.4 | Create `ProjectMember.cs` | ✅ | Join entity: `ProjectId`, `UserId` |
| 1.2.5 | Create `RetroBoard.cs` | ✅ | `ProjectId`, `Name`, navigation to `Columns` |
| 1.2.6 | Create `Column.cs` | ✅ | `RetroBoardId`, `Name`, navigation to `Notes` |
| 1.2.7 | Create `Note.cs` | ✅ | `ColumnId`, `Text`, navigation to `Votes` |
| 1.2.8 | Create `Vote.cs` | ✅ | `NoteId`, `UserId` |

> **DESIGN:** Entities live in the WebApi project itself, in an `Entities/`
> folder. There is no separate Domain project. This is intentional — in the
> Transaction Script pattern, there is no "domain layer" because entities
> carry no behavior. They are EF Core mapping targets and nothing more.

### 1.3 DTOs

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.3.1 | Create all Request record types | ✅ | Same shapes as API 1: `CreateUserRequest`, `CreateProjectRequest`, `AddMemberRequest`, `CreateRetroBoardRequest`, `CreateColumnRequest`, `UpdateColumnRequest`, `CreateNoteRequest`, `UpdateNoteRequest`, `CastVoteRequest` |
| 1.3.2 | Create all Response record types | ✅ | Same shapes as API 1: `UserResponse`, `ProjectResponse`, `ProjectMemberResponse`, `RetroBoardResponse(... Columns)`, `ColumnResponse(... Notes)`, `NoteResponse(... VoteCount)`, `VoteResponse` |

> **DESIGN:** DTOs are records in a flat `DTOs/` folder — no Requests/ and
> Responses/ subfolders. The Transaction Script approach favors fewer
> organizational boundaries. The shapes are identical to API 1's DTOs so
> the same integration test suite's shared DTOs can validate the contract.

### 1.4 Data Layer (DbContext + AuditInterceptor)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.4.1 | Create `RetroBoardDbContext.cs` | ✅ | All `DbSet<T>` properties. `OnModelCreating` has ALL entity configuration inline (keys, relationships, query filters, max lengths). No separate `IEntityTypeConfiguration` classes. |
| 1.4.2 | Create `AuditInterceptor.cs` | ✅ | Same as API 1: stamps `CreatedAt`/`LastUpdatedAt`, converts `Deleted` → soft delete. Registered as Singleton. |
| 1.4.3 | Add unique indexes for invariant safety nets | ✅ | `(RetroBoardId, Name)` on Column, `(ColumnId, Text)` on Note, `(NoteId, UserId)` on Vote — with `HasFilter("\"DeletedAt\" IS NULL")`. **These exist but Api0a does NOT catch the resulting exceptions — same blind spot as API 1.** |
| 1.4.4 | Add soft-delete global query filters on all entities | ✅ | `.HasQueryFilter(e => e.DeletedAt == null)` |

> **DESIGN:** All entity configuration lives inside `OnModelCreating` as a
> single block. This is less maintainable at scale but perfectly readable for
> 7 entities. The key teaching point: at this scale, separate
> `IEntityTypeConfiguration<T>` classes are organizational overhead with no
> functional benefit.
>
> **Important:** The unique indexes ARE present in Api0a (they're part of the
> schema), but the middleware does NOT catch `DbUpdateException` / `23505`.
> This means concurrent duplicates that bypass the application-level check
> will hit the DB constraint and bubble up as unhandled 500 errors — a
> different failure mode from API 1 (which also has these indexes but
> similarly doesn't catch constraint violations gracefully). Api0b fixes this.

### 1.5 Validators (FluentValidation)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.5.1 | Create `CreateUserRequestValidator` | ✅ | `Name` required, max 200. `Email` required, valid format. |
| 1.5.2 | Create `CreateProjectRequestValidator` | ✅ | `Name` required, max 200. |
| 1.5.3 | Create `CreateColumnRequestValidator` | ✅ | `Name` required, max 200. |
| 1.5.4 | Create `CreateNoteRequestValidator` | ✅ | `Text` required, max 2000. |
| 1.5.5 | Create `CastVoteRequestValidator` | ✅ | `UserId` must not be empty. |

### 1.6 Middleware

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.6.1 | Create `GlobalExceptionHandlerMiddleware.cs` | ✅ | Catches `NotFoundException` → 404, `DuplicateException` → 409, `BusinessRuleException` → 409. Does **NOT** catch `DbUpdateException` / `DbUpdateConcurrencyException` — this is intentional for Api0a (fixed in Api0b). Uses Problem Details (RFC 7807). |
| 1.6.2 | Create simple exception types inline or in a `Exceptions/` folder | ✅ | `NotFoundException`, `DuplicateException`, `BusinessRuleException` — same as API 1 but defined in the single project. |

### 1.7 Minimal API Endpoints

> **DESIGN:** Each endpoint group is a static class with an `IEndpointRouteBuilder`
> extension method, called from `Program.cs`. Each handler method is a
> **transaction script** — it receives the `DbContext`, performs the entire
> operation (validate → query → mutate → save), and returns a result.
> There is no service, no repository, no unit of work abstraction.

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.7.1 | Create `UserEndpoints.cs` | ✅ |
| 1.7.2 | Create `ProjectEndpoints.cs` | ✅ |
| 1.7.3 | Create `RetroBoardEndpoints.cs` | ✅ |
| 1.7.4 | Create `ColumnEndpoints.cs` | ✅ |
| 1.7.5 | Create `NoteEndpoints.cs` | ✅ |
| 1.7.6 | Create `VoteEndpoints.cs` | ✅ |

#### Endpoint Handler Example (Column Creation)

```csharp
// DESIGN: This is a Transaction Script — the entire operation is a
// self-contained script in a single method. Compare with API 1 where
// the same operation traverses: Controller → IColumnService → ColumnService
// → IColumnRepository → ColumnRepository → IUnitOfWork → UnitOfWork.
//
// The check-then-act race condition on column name uniqueness is the SAME
// as API 1's ColumnService.CreateAsync. Both are vulnerable to concurrent
// duplicate requests. Api0b fixes this at the database level.

static async Task<IResult> CreateColumn(
    Guid retroId,
    CreateColumnRequest request,
    RetroBoardDbContext db,
    CancellationToken ct)
{
    RetroBoard? retro = await db.RetroBoards.FindAsync([retroId], ct);
    if (retro is null)
        throw new NotFoundException("RetroBoard", retroId);

    bool nameExists = await db.Columns
        .AnyAsync(c => c.RetroBoardId == retroId
            && c.Name == request.Name, ct);
    if (nameExists)
        throw new DuplicateException("Column", "Name", request.Name);

    var column = new Column
    {
        Id = Guid.NewGuid(),
        RetroBoardId = retroId,
        Name = request.Name
    };

    db.Columns.Add(column);
    await db.SaveChangesAsync(ct);

    return Results.Created(
        $"/api/retros/{retroId}/columns/{column.Id}",
        new ColumnResponse(column.Id, column.Name, null));
}
```

### 1.8 Program.cs

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.8.1 | Configure DI: `AddDbContext`, `AddSingleton<AuditInterceptor>`, `AddValidatorsFromAssemblyContaining`, Swagger | ✅ | No repository or service registrations — they don't exist. |
| 1.8.2 | Configure middleware pipeline: Swagger (dev), `GlobalExceptionHandlerMiddleware` | ✅ | |
| 1.8.3 | Map all endpoint groups: `app.MapUserEndpoints()`, etc. | ✅ | |
| 1.8.4 | Add `public partial class Program { }` for test access | ✅ | Same pattern as API 1–5 for `WebApplicationFactory<Program>`. |
| 1.8.5 | Add `appsettings.json` with connection string | ✅ | Same connection string name (`RetroBoard`) as other APIs. |

### 1.9 Integration Tests (Api0a)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.9.1 | Create `tests/Api0a.IntegrationTests/` project | ✅ | Refs: `Api0a.WebApi`, `RetroBoard.IntegrationTests.Shared` |
| 1.9.2 | Create `Api0aFixture.cs` | ✅ | Extends `ApiFixture<Program>`. Overrides `ConfigureServices` to replace DB connection string. Overrides `ApplyMigrationsAsync` to call `EnsureCreatedAsync`. |
| 1.9.3 | Create `Api0aCrudTests.cs` | ✅ | Extends `CrudTestsBase<Api0aFixture>`. **Expected: ✅ All pass.** |
| 1.9.4 | Create `Api0aInvariantTests.cs` | ✅ | Extends `InvariantTestsBase<Api0aFixture>`. **Expected: ✅ All pass.** |
| 1.9.5 | Create `Api0aSoftDeleteTests.cs` | ✅ | Extends `SoftDeleteTestsBase<Api0aFixture>`. **Expected: ✅ All pass.** |
| 1.9.6 | Create `Api0aConcurrencyTests.cs` | ✅ | Extends `ConcurrencyTestsBase<Api0aFixture>`. **Expected: ❌ Both fail** (same as API 1/2). |
| 1.9.7 | Create `Api0aConsistencyBoundaryTests.cs` | ✅ | Extends `ConsistencyBoundaryTestsBase<Api0aFixture>`. **Expected: Baseline tests ✅, cross-entity tests ❌** (same as API 1/2). |
| 1.9.8 | Add test project to `RetroBoard.slnx` | ✅ | |
| 1.9.9 | Run full test suite — verify expected pass/fail pattern matches API 1 | ✅ | 18 passed, 4 failed (2 concurrency + 2 consistency boundary). Matches expected pattern. |

#### Expected Test Results (Api0a)

```
✅ CRUD Happy Path               — Pass
✅ Invariant Enforcement          — Pass (single-threaded)
✅ Soft Delete                    — Pass
❌ Concurrent Duplicate Column    — FAIL (both requests succeed)
❌ Concurrent Duplicate Vote      — FAIL (both votes created)
❌ Cross-Entity Boundary (some)   — FAIL (same as API 1/2)
```

---

## Phase 2: Api0b — Transaction Script + Database Concurrency Safety

> **Goal:** Copy Api0a into Api0b, then apply a small, targeted set of changes
> that fix all concurrency failures using **database mechanisms only**. No new
> abstractions, no new patterns, no new projects. The diff should be small
> enough to fit on a single screen.

### 2.1 Project Setup

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.1.1 | Create `src/Api0.TransactionScript/Api0b.WebApi/` by copying Api0a | ✅ | Rename namespace root to `Api0b`. Keep same folder structure. |
| 2.1.2 | Update `Api0b.WebApi.csproj` — rename, update root namespace | ✅ | |
| 2.1.3 | Add to `RetroBoard.slnx` under `/src/Api0.TransactionScript/` | ✅ | |
| 2.1.4 | Verify `dotnet build` succeeds | ✅ | 0 warnings, 0 errors across all 25 projects. |

#### What Changes (The Diff)

The diff between Api0a and Api0b is intentionally minimal. It consists of
exactly three categories of changes:

```
1. Entity configuration   → Add xmin concurrency tokens
2. Middleware              → Catch DbUpdateConcurrencyException + DbUpdateException (23505)
3. Endpoint handlers      → No changes (this is the point)
```

### 2.2 Add Concurrency Tokens (xmin)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.2.1 | Add `Version` property (`uint`) to entities that need concurrency protection | ✅ | Add to: `RetroBoard`, `Project`, `User`. These are the "root" entities whose concurrent modification should be detected. |
| 2.2.2 | Configure `xmin` mapping in `OnModelCreating` | ✅ | For each entity with `Version`: `builder.Property(e => e.Version).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();` |

> **DESIGN:** In the DDD path (API 3+), `xmin` is configured only on
> **aggregate roots** because the aggregate is the consistency boundary.
> In the Transaction Script path, we apply `xmin` to **individual entities**
> that are the "root" of a logical group. The effect is the same — EF Core
> adds a `WHERE xmin = @expected` clause to UPDATE statements — but the
> reasoning is different. There is no "aggregate" concept here; we're just
> telling the database "don't let two writers clobber each other."
>
> We do NOT add xmin to every entity (e.g., not on Vote, Column, Note
> individually). Vote uniqueness is handled by the unique index. Column/Note
> concurrency is indirectly protected because their creation/update scripts
> check parent existence and the parent has xmin. For direct Column/Note
> updates, the xmin on those entities could be added, but we keep it minimal
> to demonstrate the "just enough" philosophy.

### 2.3 Enhance Middleware — Catch DB Concurrency & Constraint Violations

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.3.1 | Add `catch (DbUpdateConcurrencyException)` → 409 Conflict | ✅ | Problem Details: `"The resource was modified by another request. Please retry."` |
| 2.3.2 | Add `catch (DbUpdateException) when (IsUniqueConstraintViolation)` → 409 Conflict | ✅ | Check `InnerException is PostgresException { SqlState: "23505" }`. Problem Details: `"A duplicate entry was detected."` |
| 2.3.3 | Add NuGet ref to `Npgsql` (for `PostgresException` type) if not already transitively available | ✅ | Comes transitively via `Npgsql.EntityFrameworkCore.PostgreSQL` — no additional reference needed. |

> **DESIGN:** This is the critical change. Api0a had unique indexes in the
> schema but didn't catch the resulting exceptions. Api0b catches them and
> returns proper 409 Conflict responses. This single middleware change is
> what makes the concurrency tests pass.
>
> Compare with the DDD path: API 3 needed aggregate roots, per-aggregate
> repositories, `xmin` on aggregate roots, full graph loading via
> `.Include()` chains, and rich domain methods — all to achieve the same
> observable HTTP behavior that Api0b achieves with two `catch` blocks.

### 2.4 Endpoint Handlers — No Changes Required

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.4.1 | Verify that **no endpoint handler code changes** are needed | ✅ | Verified: all 6 endpoint files are identical to Api0a (namespace change only). The check-then-act code stays the same. The DB constraints handle the concurrent case. |

> **DESIGN:** The endpoint handler code in Api0b is **identical** to Api0a.
> The concurrency fix is entirely in configuration (xmin tokens) and
> infrastructure (middleware). The business logic didn't change — the safety
> net did. This is the most powerful visual argument in the entire repository
> for "let the database handle it."

### 2.5 Integration Tests (Api0b)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.5.1 | Create `tests/Api0b.IntegrationTests/` project | ✅ | Refs: `Api0b.WebApi`, `RetroBoard.IntegrationTests.Shared` |
| 2.5.2 | Create `Api0bFixture.cs` | ✅ | Same structure as Api0a fixture but references Api0b's `Program` and `RetroBoardDbContext`. |
| 2.5.3 | Create `Api0bCrudTests.cs` | ✅ | **Expected: ✅ All pass.** |
| 2.5.4 | Create `Api0bInvariantTests.cs` | ✅ | **Expected: ✅ All pass.** |
| 2.5.5 | Create `Api0bSoftDeleteTests.cs` | ✅ | **Expected: ✅ All pass.** |
| 2.5.6 | Create `Api0bConcurrencyTests.cs` | ✅ | **Expected: ✅ Both pass** — this is the milestone. |
| 2.5.7 | Create `Api0bConsistencyBoundaryTests.cs` | ✅ | **Expected: Baseline ✅, cross-entity ❌** (same as Api0a — no aggregate boundaries). |
| 2.5.8 | Add test project to `RetroBoard.slnx` | ✅ | |
| 2.5.9 | Run full test suite — verify concurrency tests now pass | ✅ | 20 passed, 2 failed (consistency boundary only). Concurrency tests PASS — milestone achieved! |

#### Expected Test Results (Api0b)

```
✅ CRUD Happy Path               — Pass
✅ Invariant Enforcement          — Pass
✅ Soft Delete                    — Pass
✅ Concurrent Duplicate Column    — PASS (DB unique constraint → 409)
✅ Concurrent Duplicate Vote      — PASS (DB unique constraint → 409)
❌ Cross-Entity Boundary (some)   — FAIL (no aggregate boundaries — intentional)
```

#### The Diff Summary

The total diff between Api0a and Api0b should consist of approximately:

| File | Change | Lines |
|------|--------|-------|
| Entity files (3) | Add `public uint Version { get; set; }` property | ~3 lines |
| `RetroBoardDbContext.cs` | Add xmin configuration for 3 entities | ~12 lines |
| `GlobalExceptionHandlerMiddleware.cs` | Add 2 catch blocks + helper method | ~20 lines |
| **Total** | | **~35 lines** |

---

## Phase 3: Documentation

> **Goal:** Update all documentation to include API 0 in comparison tables,
> add a dedicated migration guide, and create a pattern page for Transaction
> Script.

### 3.1 New Documentation Pages

| # | Task | Status | Notes |
|---|------|--------|-------|
| 3.1.1 | Create `docfx/migration/api0-transaction-script.md` | ✅ | Migration guide for Api0a → Api0b. Structure mirrors existing tier guides: What This Tier Shows, Structure, How Business Rules Work, What's Wrong (Api0a), What's Fixed (Api0b), Concurrency Test Results, What You Trade Away, When to Use This. |
| 3.1.2 | Create `docfx/patterns/transaction-script-pattern.md` | ✅ | Pattern explanation: definition (Fowler), how it maps to this codebase, code example, comparison with Repository+Service+UoW, trade-offs table. |
| 3.1.3 | Add entries to `docfx/migration/toc.yml` | ✅ | Add Api0 entry **before** Api1 in the list. |
| 3.1.4 | Add entry to `docfx/patterns/toc.yml` | ✅ | Add Transaction Script pattern entry. |

### 3.2 Update Existing Documentation

| # | Task | Status | Notes |
|---|------|--------|-------|
| 3.2.1 | Update `docfx/migration/index.md` — add Api0 to the journey diagram | ✅ | Show Api0 as a parallel track, not a step in the linear path. Add to the Quick Comparison table. |
| 3.2.2 | Update `docfx/architecture/project-structure.md` — add Api0 folder | ✅ | Show single-project structure, explain why it breaks the 4-layer convention. |
| 3.2.3 | Update `docfx/concepts/concurrency.md` — add Api0a/0b rows to tier table | ✅ | Api0a: None (❌/❌). Api0b: DB constraints + xmin (✅/✅). |
| 3.2.4 | Update `README.md` — add Api0 section and update comparison tables | ✅ | Add a new section before "API 1" explaining the parallel track. Update tier comparison table. Update testing table. Update repository structure diagram. |
| 3.2.5 | Update `docs/DesignDecisions.md` — add Api0 to all decision tables | ✅ | Entity Design, Business Logic Location, Repository Granularity, Concurrency Control, and Summary tables. |

### 3.3 Migration Guide Content (`api0-transaction-script.md`)

The migration guide should cover:

#### Api0a Section
- **What This Tier Shows:** The simplest possible implementation. One project. No abstractions beyond the DbContext.
- **Structure:** Single-project diagram (contrast with API 1's 4-project structure).
- **How Business Rules Work:** Transaction scripts — inline in endpoint handlers. Same check-then-act pattern as API 1's services, but without the service/repository indirection.
- **What's Wrong:** Same concurrency failures as API 1/2. No consistency boundaries. No domain expressiveness. No unit testability of business logic (it's coupled to DbContext).
- **Concurrency Test Results:** Same failures as API 1.

#### Api0b Section
- **What Changes:** ~35 lines of configuration and middleware.
- **The Fix:** xmin tokens + catch DB exceptions in middleware.
- **Why This Works:** The database is the consistency boundary. Unique indexes prevent duplicates atomically. xmin prevents lost updates.
- **Concurrency Test Results:** Concurrency tests pass. Consistency boundary tests still fail (intentional — no aggregate concept).

#### What You Trade Away
| Lost Capability | Why It Might Matter |
|----------------|-------------------|
| Unit-testable domain logic | Business rules are in endpoint handlers coupled to DbContext — can't test without a database |
| Encapsulation | Any code can modify any entity's properties — no private setters, no guard methods |
| Domain expressiveness | Rules are "hidden" in SQL queries and DB constraints, not explicit in code |
| Aggregate boundaries | No concept of consistency boundaries — cross-entity rules must be manually coordinated |
| Separation of concerns | HTTP, business logic, and data access are all in the same method |
| Scalable team structure | Hard for multiple developers to work on the same endpoint group without conflicts |

#### When to Use This
| Scenario | Recommendation |
|----------|---------------|
| Prototype, hackathon, MVP | ✅ Api0 — speed over structure |
| Small team (1–3), simple domain, < 20 endpoints | ✅ Api0b — simple with safety |
| Medium team, growing domain, multiple aggregates | ⚠️ Start considering API 2/3 |
| Large team, complex domain, high concurrency | ❌ Use API 3–5 patterns |

---

## Phase 4: Update Comparison Tables & Cross-References

> **Goal:** Ensure every existing comparison in the repo includes Api0.

### 4.1 Comparison Table Updates

| # | Task | Status | Notes |
|---|------|--------|-------|
| 4.1.1 | `README.md` — Tier Comparison table: add Api0a and Api0b columns | ✅ | Done in Phase 3. |
| 4.1.2 | `README.md` — Testing Strategy table: add Api0a and Api0b rows | ✅ | Done in Phase 3. |
| 4.1.3 | `README.md` — Repository Structure: add Api0 folders | ✅ | Done in Phase 3. |
| 4.1.4 | `docfx/migration/index.md` — Quick Comparison table: add Api0 columns | ✅ | Done in Phase 3. |
| 4.1.5 | `docfx/concepts/concurrency.md` — Concurrency at Each Tier table | ✅ | Done in Phase 3. |
| 4.1.6 | `docs/DesignDecisions.md` — All decision tables (5 tables) | ✅ | Done in Phase 3. |

### 4.2 Cross-Reference Updates

| # | Task | Status | Notes |
|---|------|--------|-------|
| 4.2.1 | `docfx/migration/api1-anemic-crud.md` — add a "See Also" note pointing to Api0 | ✅ | Done in Phase 3. |
| 4.2.2 | `docfx/architecture/clean-architecture.md` — add note about when 4 layers aren't needed | ✅ | Done in Phase 3. Added "When Four Layers Aren't Needed" section. |
| 4.2.3 | `docfx/patterns/repository-pattern.md` — add contrast with Transaction Script | ✅ | Done in Phase 3. |
| 4.2.4 | `docfx/patterns/unit-of-work-pattern.md` — add note about Api0's direct SaveChanges | ✅ | Done in Phase 3. |
| 4.2.5 | `.github/copilot-instructions.md` — add Api0 to Tier-Specific Reminders | ✅ | Done in Phase 3. |

---

## Phase 5: Final Validation

> **Goal:** Ensure everything compiles, all tests have the expected pass/fail
> pattern, and documentation is consistent.

### 5.1 Build & Test Validation

| # | Task | Status | Notes |
|---|------|--------|-------|
| 5.1.1 | `dotnet build` — entire solution compiles with zero warnings | ✅ | 0 warnings, 0 errors across 25 projects. |
| 5.1.2 | Run Api0a integration tests — verify expected failures | ✅ | 18P/4F: CRUD ✅, Invariant ✅, SoftDelete ✅, Concurrency ❌ (2 fail), ConsistencyBoundary (2P/2F). |
| 5.1.3 | Run Api0b integration tests — verify concurrency tests pass | ✅ | 20P/2F: CRUD ✅, Invariant ✅, SoftDelete ✅, Concurrency ✅ (both pass!), ConsistencyBoundary (2P/2F). |
| 5.1.4 | Run ALL existing API tests — confirm no regressions | ✅ | Api1: 18P/4F, Api2: 18P/4F, Api3: 22P/0F, Api4: 22P/0F, Api5: 26P/0F. Domain unit tests: all pass. |
| 5.1.5 | Verify shared test DTOs match Api0's JSON contract | ✅ | SharedDTOs.cs shape exactly matches Api0 response records. |

### 5.2 Documentation Validation

| # | Task | Status | Notes |
|---|------|--------|-------|
| 5.2.1 | Build DocFX site locally — verify no broken links | ✅ | `docfx build` succeeded. 2 pre-existing warnings (not Api0-related). |
| 5.2.2 | Review all updated comparison tables for consistency | ✅ | Api0 present in README.md, index.md, concurrency.md, DesignDecisions.md tables. |
| 5.2.3 | Proof-read `api0-transaction-script.md` for accuracy | ✅ | All code snippets match actual implementation. |
| 5.2.4 | Verify `DESIGN:` comments in Api0 code reference docfx concepts | ✅ | 43 DESIGN: comments across Api0a/0b. All themes covered in docfx pages. |

### 5.3 Code Quality

| # | Task | Status | Notes |
|---|------|--------|-------|
| 5.3.1 | All public types have XML doc comments | ✅ | All public types in Api0a and Api0b have `<summary>` XML doc comments. |
| 5.3.2 | All `DESIGN:` comments explain why this tier does it this way | ✅ | All 43 comments explain tier-specific reasoning (pattern choice, concurrency, comparisons). |
| 5.3.3 | `CancellationToken` passed through all async chains | ✅ | All EF Core async calls pass `ct`. Middleware uses standard ASP.NET Core signature. |
| 5.3.4 | No `var` for non-obvious types | ✅ | All `var` usages are with `new` expressions (type obvious from RHS). |
| 5.3.5 | No NuGet packages added outside `Directory.Packages.props` | ✅ | No `Version` attributes in csproj files. All 7 packages exist in central props. |

---

## Appendix A: Updated Comparison Tables (Preview)

### Tier Comparison (for README.md and migration/index.md)

| Aspect | Api0a | Api0b | API 1 | API 2 | API 3 | API 4 | API 5 |
|--------|:-----:|:-----:|:-----:|:-----:|:-----:|:-----:|:-----:|
| Projects | 1 | 1 | 4 | 4 | 4 | 4 | 4 |
| API style | Minimal APIs | Minimal APIs | Controllers | Controllers | Controllers | Controllers | Controllers |
| Pattern | Transaction Script | Transaction Script | Layered CRUD | Rich Domain | Aggregates | Split Aggregates | CQRS + MediatR |
| Business logic in | Endpoint handlers | Endpoint handlers | Services | Entities | Aggregate roots | Aggregate roots | Handlers + Domain |
| Repository count | 0 | 0 | 7 | 7 | 3 | 4 | 4 |
| Service/Handler count | 0 | 0 | 7 | 7 | 3 | 4 | 13+ handlers |
| Concurrency safe? | ❌ | ✅ (DB) | ❌ | ❌ | ✅ (Aggregate) | ✅ (Aggregate) | ✅ (Aggregate) |
| Consistency boundary | ❌ None | ❌ None | ❌ None | ❌ None | ✅ Aggregate | ✅ Aggregate | ✅ Aggregate |
| Unit testable domain? | ❌ | ❌ | ❌ (anemic) | ✅ | ✅ | ✅ | ✅ |
| Reads optimized? | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ (CQRS) |

### Concurrency Comparison (for concepts/concurrency.md)

| Tier | Strategy | Concurrent Duplicate Column | Concurrent Duplicate Vote |
|------|----------|---------------------------|--------------------------|
| Api0a | None | ❌ Both succeed (duplicate!) | ❌ Both succeed |
| Api0b | DB constraints + xmin | ✅ DB unique constraint → 409 | ✅ DB unique constraint → 409 |
| API 1 | None | ❌ Both succeed (duplicate!) | ❌ Both succeed |
| API 2 | None | ❌ Both succeed (duplicate!) | ❌ Both succeed |
| API 3 | Optimistic (xmin on aggregate root) | ✅ One fails with 409 | ✅ One fails with 409 |
| API 4 | Optimistic (xmin) + DB constraint | ✅ One fails with 409 | ✅ DB unique constraint |
| API 5 | Same as API 4 | ✅ One fails with 409 | ✅ DB unique constraint |

### Testing Results (for README.md)

| Test Category | Api0a | Api0b | API 1 | API 2 | API 3 | API 4 | API 5 |
|--------------|:-----:|:-----:|:-----:|:-----:|:-----:|:-----:|:-----:|
| CRUD happy path | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Invariant enforcement | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Soft delete | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Concurrency conflicts | ❌ | ✅ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Consistency under load | ❌ | ❌ | ❌ | ❌ | ✅ | ✅ | ✅ |
| Domain unit tests | ⏭️ N/A | ⏭️ N/A | ⏭️ N/A | ✅ ~29 | ✅ ~25 | ✅ ~23 | ✅ ~76 |

---

## Appendix B: Design Decisions Specific to API 0

### Why Minimal APIs Instead of Controllers?

API 1–5 all use `[ApiController]` controllers. Api0 uses Minimal APIs to
maximize the contrast:

| Aspect | Controllers (API 1–5) | Minimal APIs (Api0) |
|--------|----------------------|-------------------|
| Boilerplate | Class + constructor + DI + attributes | Static method + lambda |
| DI | Constructor injection | Parameter injection |
| Routing | `[Route]` + `[HttpGet]` attributes | `app.MapGet("/path", handler)` |
| Grouping | One class per resource | One static class per resource (by convention) |
| Model binding | Automatic via `[FromBody]`, `[FromRoute]` | Automatic (inferred) |

Minimal APIs also demonstrate a feature of ASP.NET Core that the main
path doesn't cover — broadening the educational scope.

### Why NOT Use Minimal APIs for API 1–5?

The main path uses controllers because:
1. They're more familiar to the target audience (junior → mid-level devs).
2. They match the Clean Architecture convention of a "thin controller" layer.
3. Switching API style AND architecture simultaneously would muddy the lesson.

Api0 can use Minimal APIs because there are no architectural layers to
coordinate — the simplicity of the API style matches the simplicity of the
architecture.

### Why Keep Separate Api0a and Api0b Projects?

Instead of a single project with a flag, we use two separate projects
because:
1. **Consistency with the repo's philosophy** — each API is independently
   runnable and diffable.
2. **The diff is the lesson** — readers should `diff Api0a Api0b` to see
   exactly what changed.
3. **Integration tests validate both states** — Api0a's failures and Api0b's
   fixes are both captured in the test suite.

---

## Appendix C: Files to Create (Complete List)

### Api0a (Phase 1)

| # | File Path | Description |
|---|-----------|-------------|
| 1 | `src/Api0.TransactionScript/Api0a.WebApi/Api0a.WebApi.csproj` | Project file |
| 2 | `src/Api0.TransactionScript/Api0a.WebApi/Program.cs` | Entry point |
| 3 | `src/Api0.TransactionScript/Api0a.WebApi/appsettings.json` | Configuration |
| 4 | `src/Api0.TransactionScript/Api0a.WebApi/Entities/AuditableEntityBase.cs` | Base entity |
| 5 | `src/Api0.TransactionScript/Api0a.WebApi/Entities/User.cs` | User entity |
| 6 | `src/Api0.TransactionScript/Api0a.WebApi/Entities/Project.cs` | Project entity |
| 7 | `src/Api0.TransactionScript/Api0a.WebApi/Entities/ProjectMember.cs` | ProjectMember entity |
| 8 | `src/Api0.TransactionScript/Api0a.WebApi/Entities/RetroBoard.cs` | RetroBoard entity |
| 9 | `src/Api0.TransactionScript/Api0a.WebApi/Entities/Column.cs` | Column entity |
| 10 | `src/Api0.TransactionScript/Api0a.WebApi/Entities/Note.cs` | Note entity |
| 11 | `src/Api0.TransactionScript/Api0a.WebApi/Entities/Vote.cs` | Vote entity |
| 12 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/CreateUserRequest.cs` | DTO |
| 13 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/CreateProjectRequest.cs` | DTO |
| 14 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/AddMemberRequest.cs` | DTO |
| 15 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/CreateRetroBoardRequest.cs` | DTO |
| 16 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/CreateColumnRequest.cs` | DTO |
| 17 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/UpdateColumnRequest.cs` | DTO |
| 18 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/CreateNoteRequest.cs` | DTO |
| 19 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/UpdateNoteRequest.cs` | DTO |
| 20 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/CastVoteRequest.cs` | DTO |
| 21 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/UserResponse.cs` | DTO |
| 22 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/ProjectResponse.cs` | DTO |
| 23 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/ProjectMemberResponse.cs` | DTO |
| 24 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/RetroBoardResponse.cs` | DTO |
| 25 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/ColumnResponse.cs` | DTO |
| 26 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/NoteResponse.cs` | DTO |
| 27 | `src/Api0.TransactionScript/Api0a.WebApi/DTOs/VoteResponse.cs` | DTO |
| 28 | `src/Api0.TransactionScript/Api0a.WebApi/Data/RetroBoardDbContext.cs` | DbContext |
| 29 | `src/Api0.TransactionScript/Api0a.WebApi/Data/Interceptors/AuditInterceptor.cs` | Interceptor |
| 30 | `src/Api0.TransactionScript/Api0a.WebApi/Endpoints/UserEndpoints.cs` | Endpoints |
| 31 | `src/Api0.TransactionScript/Api0a.WebApi/Endpoints/ProjectEndpoints.cs` | Endpoints |
| 32 | `src/Api0.TransactionScript/Api0a.WebApi/Endpoints/RetroBoardEndpoints.cs` | Endpoints |
| 33 | `src/Api0.TransactionScript/Api0a.WebApi/Endpoints/ColumnEndpoints.cs` | Endpoints |
| 34 | `src/Api0.TransactionScript/Api0a.WebApi/Endpoints/NoteEndpoints.cs` | Endpoints |
| 35 | `src/Api0.TransactionScript/Api0a.WebApi/Endpoints/VoteEndpoints.cs` | Endpoints |
| 36 | `src/Api0.TransactionScript/Api0a.WebApi/Middleware/GlobalExceptionHandlerMiddleware.cs` | Middleware |
| 37 | `src/Api0.TransactionScript/Api0a.WebApi/Exceptions/NotFoundException.cs` | Exception |
| 38 | `src/Api0.TransactionScript/Api0a.WebApi/Exceptions/DuplicateException.cs` | Exception |
| 39 | `src/Api0.TransactionScript/Api0a.WebApi/Exceptions/BusinessRuleException.cs` | Exception |
| 40 | `src/Api0.TransactionScript/Api0a.WebApi/Validators/CreateUserRequestValidator.cs` | Validator |
| 41 | `src/Api0.TransactionScript/Api0a.WebApi/Validators/CreateProjectRequestValidator.cs` | Validator |
| 42 | `src/Api0.TransactionScript/Api0a.WebApi/Validators/CreateColumnRequestValidator.cs` | Validator |
| 43 | `src/Api0.TransactionScript/Api0a.WebApi/Validators/CreateNoteRequestValidator.cs` | Validator |
| 44 | `src/Api0.TransactionScript/Api0a.WebApi/Validators/CastVoteRequestValidator.cs` | Validator |
| 45 | `tests/Api0a.IntegrationTests/Api0a.IntegrationTests.csproj` | Test project |
| 46 | `tests/Api0a.IntegrationTests/Fixtures/Api0aFixture.cs` | Test fixture |
| 47 | `tests/Api0a.IntegrationTests/Tests/Api0aCrudTests.cs` | Tests |
| 48 | `tests/Api0a.IntegrationTests/Tests/Api0aInvariantTests.cs` | Tests |
| 49 | `tests/Api0a.IntegrationTests/Tests/Api0aSoftDeleteTests.cs` | Tests |
| 50 | `tests/Api0a.IntegrationTests/Tests/Api0aConcurrencyTests.cs` | Tests |
| 51 | `tests/Api0a.IntegrationTests/Tests/Api0aConsistencyBoundaryTests.cs` | Tests |

### Api0b (Phase 2) — Copy of Api0a with modifications

| # | File Path | Description |
|---|-----------|-------------|
| 1–44 | `src/Api0.TransactionScript/Api0b.WebApi/*` | Copy of Api0a with namespace rename + concurrency changes |
| 45 | `tests/Api0b.IntegrationTests/Api0b.IntegrationTests.csproj` | Test project |
| 46 | `tests/Api0b.IntegrationTests/Fixtures/Api0bFixture.cs` | Test fixture |
| 47 | `tests/Api0b.IntegrationTests/Tests/Api0bCrudTests.cs` | Tests |
| 48 | `tests/Api0b.IntegrationTests/Tests/Api0bInvariantTests.cs` | Tests |
| 49 | `tests/Api0b.IntegrationTests/Tests/Api0bSoftDeleteTests.cs` | Tests |
| 50 | `tests/Api0b.IntegrationTests/Tests/Api0bConcurrencyTests.cs` | Tests |
| 51 | `tests/Api0b.IntegrationTests/Tests/Api0bConsistencyBoundaryTests.cs` | Tests |

### Documentation (Phase 3–4)

| # | File Path | Action |
|---|-----------|--------|
| 1 | `docfx/migration/api0-transaction-script.md` | Create |
| 2 | `docfx/patterns/transaction-script-pattern.md` | Create |
| 3 | `docfx/migration/toc.yml` | Update |
| 4 | `docfx/patterns/toc.yml` | Update |
| 5 | `docfx/migration/index.md` | Update |
| 6 | `docfx/architecture/project-structure.md` | Update |
| 7 | `docfx/concepts/concurrency.md` | Update |
| 8 | `docfx/migration/api1-anemic-crud.md` | Update |
| 9 | `docfx/architecture/clean-architecture.md` | Update |
| 10 | `docfx/patterns/repository-pattern.md` | Update |
| 11 | `docfx/patterns/unit-of-work-pattern.md` | Update |
| 12 | `README.md` | Update |
| 13 | `docs/DesignDecisions.md` | Update |
| 14 | `.github/copilot-instructions.md` | Update |
