# API 0 — Transaction Script

> **Pattern:** Endpoint handler → DbContext → Database (direct, no layers)

## Parallel Track — Not Part of the Linear Path

API 0 is **not** a step in the API 1 → 5 evolutionary progression. It exists
as a **parallel track** — a side-by-side comparison that answers the question:

> *"What if I just kept it simple?"*

API 0 comes in two versions:

```
Api0a: Transaction Script             "Simple. Same concurrency failures as API 1/2."
  │
  ▼  Add xmin tokens + catch DB exceptions (~35 lines of diff)
Api0b: Transaction Script + DB         "Same simplicity. Concurrency fixed."
       Concurrency Safety
```

Contrast with the main DDD path where fixing the same concurrency tests
required **three full rewrites** across 12+ projects (API 1 → 2 → 3).

---

## What This Tier Shows

1. **How little code is needed** to implement the same REST contract that
   API 1 spreads across 4 projects, 7 repositories, 7 services, and a UoW.
2. **That concurrency problems can be fixed at the database level** without
   aggregates, rich domain models, or architectural rewrites.
3. **When simplicity is the right choice** — and when it stops being enough.

## Structure

```
Api0a.WebApi/   (or Api0b.WebApi/)
├── Entities/              ← Anemic entities (same shape as API 1)
├── DTOs/                  ← All request/response records in one file
├── Data/
│   ├── RetroBoardDbContext.cs    ← Inline OnModelCreating (no IEntityTypeConfiguration classes)
│   └── Interceptors/
│       └── AuditInterceptor.cs   ← Soft delete + timestamps
├── Endpoints/             ← Minimal API endpoint groups (Transaction Scripts)
├── Middleware/             ← GlobalExceptionHandlerMiddleware
├── Validators/            ← FluentValidation (all in one file)
├── Exceptions/            ← NotFoundException, DuplicateException, BusinessRuleException
├── Program.cs             ← Minimal DI: DbContext + Interceptor + Validators
└── appsettings.json
```

Compare with API 1's structure:

```
Api1.Domain/           ← Entities, base classes
Api1.Application/      ← Services (×7), DTOs, Validators
Api1.Infrastructure/   ← DbContext, Configs (×7), Repos (×7), UoW, Interceptors
Api1.WebApi/           ← Controllers, Middleware, Program.cs
```

**One project** vs. **four projects**. Same REST contract. Same test suite.

## How Business Rules Work

All invariant checks live directly in the endpoint handlers — each handler
is a **Transaction Script** (Fowler, *PoEAA*):

```csharp
// Api0a (and Api0b) — ColumnEndpoints.CreateColumn
static async Task<IResult> CreateColumn(
    Guid retroId,
    CreateColumnRequest request,
    RetroBoardDbContext db,
    CancellationToken ct)
{
    _ = await db.RetroBoards.FindAsync([retroId], ct)
        ?? throw new NotFoundException("RetroBoard", retroId);

    bool nameExists = await db.Columns
        .AnyAsync(c => c.RetroBoardId == retroId && c.Name == request.Name, ct);
    if (nameExists)
        throw new DuplicateException("Column", "Name", request.Name);

    var column = new Column { RetroBoardId = retroId, Name = request.Name };
    db.Columns.Add(column);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/api/retros/{retroId}/columns/{column.Id}",
        new ColumnResponse(column.Id, column.Name, null));
}
```

Compare with API 1 where the same operation traverses:
**Controller → IColumnService → ColumnService → IColumnRepository →
ColumnRepository → IUnitOfWork → UnitOfWork.**

The logic is identical. The code is ~60% shorter.

---

## Api0a: What's Wrong (Intentionally)

| Problem | Description |
|---------|------------|
| **Race conditions** | Check-then-act: two concurrent requests can both pass the uniqueness check |
| **No concurrency control** | No `xmin` tokens — last write wins silently |
| **Unhandled DB exceptions** | Unique indexes exist but the middleware doesn't catch `DbUpdateException` — concurrent violations return 500 |
| **No consistency boundaries** | No aggregate concept — cross-entity rules are not enforced |
| **No unit-testable domain** | Business logic is coupled to `DbContext` — can't test without a database |
| **No encapsulation** | Public setters on all entities — any code can mutate anything |

### Api0a Concurrency Test Results

```
✅ CRUD Happy Path               — Pass
✅ Invariant Enforcement          — Pass (single-threaded)
✅ Soft Delete                    — Pass
❌ Concurrent Duplicate Column    — FAIL (both requests succeed)
❌ Concurrent Duplicate Vote      — FAIL (both votes created)
❌ Cross-Entity Boundary (some)   — FAIL (same as API 1/2)
```

---

## Api0b: The Fix (~35 Lines of Diff)

Api0b is a copy of Api0a with three categories of targeted changes:

### 1. Entity Configuration — Add xmin Concurrency Tokens

Add a `Version` property (`uint`) mapped to PostgreSQL's `xmin` system
column on `User`, `Project`, and `RetroBoard`:

```csharp
// Api0b — User.cs (one added property)
public class User : AuditableEntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();

    // NEW: concurrency token mapped to PostgreSQL's xmin
    public uint Version { get; set; }
}
```

```csharp
// Api0b — RetroBoardDbContext.OnModelCreating (added lines per entity)
builder.Property(u => u.Version)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

### 2. Middleware — Catch DB Concurrency & Constraint Violations

Two new `catch` blocks plus a helper method:

```csharp
// Api0b — GlobalExceptionHandlerMiddleware (added catch blocks)
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex, "Concurrency conflict: {Message}", ex.Message);
    await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict,
        "Concurrency Conflict",
        "The resource was modified by another request. Please retry.");
}
catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
{
    _logger.LogWarning(ex, "Unique constraint violation: {Message}",
        ex.InnerException?.Message ?? ex.Message);
    await WriteProblemDetailsAsync(context, HttpStatusCode.Conflict,
        "Duplicate Detected",
        "A duplicate entry was detected. The operation conflicts with an existing record.");
}

// Helper
private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
    ex.InnerException is PostgresException pgEx
    && pgEx.SqlState == PostgresErrorCodes.UniqueViolation;
```

### 3. Endpoint Handlers — No Changes Required

The endpoint handler code in Api0b is **identical** to Api0a. The
concurrency fix is entirely in configuration (xmin tokens) and
infrastructure (middleware). The business logic didn't change — the safety
net did.

### Api0b Concurrency Test Results

```
✅ CRUD Happy Path               — Pass
✅ Invariant Enforcement          — Pass
✅ Soft Delete                    — Pass
✅ Concurrent Duplicate Column    — PASS (DB unique constraint → 409)
✅ Concurrent Duplicate Vote      — PASS (DB unique constraint → 409)
❌ Cross-Entity Boundary (some)   — FAIL (no aggregate boundaries — intentional)
```

---

## Why This Works

The database is the consistency boundary:

- **Unique indexes** prevent duplicates atomically — no race condition can
  bypass them. When two concurrent INSERTs both pass the application-level
  check, the database rejects the second one.
- **xmin tokens** prevent lost updates — EF Core adds
  `WHERE xmin = @expected` to UPDATE statements. If another transaction
  modified the row, the WHERE matches zero rows and EF Core throws
  `DbUpdateConcurrencyException`.
- The **middleware** catches both exception types and returns proper
  409 Conflict responses with Problem Details.

Compare with the DDD path: API 3 needed aggregate roots, per-aggregate
repositories, `xmin` on aggregate roots, full graph loading via
`.Include()` chains, and rich domain methods — all to achieve the same
observable HTTP behavior that Api0b achieves with two `catch` blocks.

---

## What You Trade Away

| Lost Capability | Why It Might Matter |
|----------------|-------------------|
| Unit-testable domain logic | Business rules are in endpoint handlers coupled to DbContext — can't test without a database |
| Encapsulation | Any code can modify any entity's properties — no private setters, no guard methods |
| Domain expressiveness | Rules are "hidden" in SQL queries and DB constraints, not explicit in code |
| Aggregate boundaries | No concept of consistency boundaries — cross-entity rules must be manually coordinated |
| Separation of concerns | HTTP, business logic, and data access are all in the same method |
| Scalable team structure | Hard for multiple developers to work on the same endpoint group without conflicts |

## When to Use This

| Scenario | Recommendation |
|----------|---------------|
| Prototype, hackathon, MVP | ✅ Api0 — speed over structure |
| Small team (1–3), simple domain, < 20 endpoints | ✅ Api0b — simple with safety |
| Medium team, growing domain, multiple aggregates | ⚠️ Start considering API 2/3 |
| Large team, complex domain, high concurrency | ❌ Use API 3–5 patterns |

## What's Different in API 1

→ [API 1 — Anemic CRUD](api1-anemic-crud.md): Same domain, same business
rules, but spread across 4 projects with 7 repositories, 7 services, and a
Unit of Work. The layered structure provides better separation of concerns
and testability foundations, but at significant organizational cost.
