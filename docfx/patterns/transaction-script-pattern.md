# Transaction Script Pattern

## Intent

Organize business logic as a **single procedure** that handles one request
from presentation to database. Each procedure (script) contains the complete
sequence: validate input, query data, apply business rules, mutate state,
persist changes, and return the result.

> *"A Transaction Script organizes all [the] logic primarily as a single
> procedure, making calls directly to the database or through a thin database
> wrapper."*
> — Martin Fowler, *Patterns of Enterprise Application Architecture*

## The Pattern

```csharp
// Api0a/Api0b — ColumnEndpoints.CreateColumn
static async Task<IResult> CreateColumn(
    Guid retroId,
    CreateColumnRequest request,
    RetroBoardDbContext db,
    CancellationToken ct)
{
    // 1. Validate existence
    _ = await db.RetroBoards.FindAsync([retroId], ct)
        ?? throw new NotFoundException("RetroBoard", retroId);

    // 2. Check invariant
    bool nameExists = await db.Columns
        .AnyAsync(c => c.RetroBoardId == retroId && c.Name == request.Name, ct);
    if (nameExists)
        throw new DuplicateException("Column", "Name", request.Name);

    // 3. Create entity and persist
    var column = new Column { RetroBoardId = retroId, Name = request.Name };
    db.Columns.Add(column);
    await db.SaveChangesAsync(ct);

    // 4. Return result
    return Results.Created($"/api/retros/{retroId}/columns/{column.Id}",
        new ColumnResponse(column.Id, column.Name, null));
}
```

The entire operation — from HTTP request to database write — is a
self-contained "script" in one method.

## How It Maps to This Codebase

API 0 (both Api0a and Api0b) uses the Transaction Script pattern:

| Component | Implementation |
|-----------|---------------|
| Script location | Minimal API endpoint handlers (`Endpoints/*.cs`) |
| Database wrapper | EF Core `DbContext` (injected directly) |
| Entities | Anemic property bags (no behavior) |
| Validation | FluentValidation (auto-validated before handler) |
| Error handling | Middleware catches exceptions → Problem Details |
| Transaction boundary | EF Core's implicit transaction on `SaveChangesAsync` |

Each endpoint handler receives the `RetroBoardDbContext` directly — there
is no repository, service, or unit of work abstraction in between.

## Comparison: Transaction Script vs. Repository + Service + UoW

The same "create column" operation across patterns:

### Transaction Script (API 0)

```
HTTP Request → Endpoint Handler → DbContext → Database
```

One method, one file, one project.

### Repository + Service + UoW (API 1)

```
HTTP Request → Controller → IColumnService → ColumnService
    → IColumnRepository → ColumnRepository → DbContext → Database
    → IUnitOfWork → UnitOfWork → DbContext.SaveChanges
```

Six classes across four projects.

### Aggregate Root (API 3)

```
HTTP Request → Controller → IRetroBoardService → RetroBoardService
    → IRetroBoardRepository → RetroBoardRepository → DbContext → Database
    → RetroBoard.AddColumn() [domain logic]
    → IUnitOfWork → UnitOfWork → DbContext.SaveChanges
```

Fewer classes than API 1, but the aggregate root contains the business rule.

### Comparison Table

| Aspect | Transaction Script (API 0) | Repository + Service (API 1) | Aggregate (API 3) |
|--------|---------------------------|-----------------------------|--------------------|
| Files involved | 1 | 6+ | 5+ |
| Projects | 1 | 4 | 4 |
| Business logic in | Endpoint handler | Service class | Aggregate root |
| DB access through | DbContext directly | Repository abstraction | Repository abstraction |
| Transaction control | Implicit (SaveChangesAsync) | UoW.SaveChangesAsync | UoW.SaveChangesAsync |
| Unit testable rules? | ❌ (coupled to DbContext) | ❌ (coupled to repo/UoW) | ✅ (pure domain method) |
| Concurrency control | DB constraints (Api0b) | ❌ None | xmin on aggregate root |

## Trade-offs

### Advantages

- **Simplicity** — One method does everything. Easy to read, easy to debug.
- **Speed of development** — No abstractions to wire up. Add an endpoint,
  write the script, done.
- **Low ceremony** — No interfaces, no DI registrations for repos/services,
  no separate configuration classes.
- **Fewer files** — Api0 has ~18 source files. API 1 has ~50+.

### Disadvantages

- **No encapsulation** — Business rules are scattered across endpoint
  handlers. The same "column name uniqueness" rule is repeated in both
  `CreateColumn` and `UpdateColumn`.
- **No unit testability** — Business logic is coupled to `DbContext`.
  Testing requires a real database (integration tests only).
- **No domain expressiveness** — The "what" (business rules) is mixed with
  the "how" (SQL queries, HTTP responses).
- **Scales poorly** — As the domain grows, endpoint handlers become long
  and repetitive. Multiple developers editing the same file create merge
  conflicts.
- **Cross-cutting concerns are manual** — Logging, transactions, and
  validation must be handled in each handler (or via middleware).

## When to Use

| Scenario | Recommendation |
|----------|---------------|
| Prototype / hackathon / MVP | ✅ Transaction Script — maximize speed |
| Small CRUD app, 1–3 developers | ✅ Transaction Script (with DB safety — Api0b) |
| Growing domain, multiple invariants | ⚠️ Consider Rich Domain (API 2) |
| Multiple developers, concurrent writes | ❌ Use Aggregates (API 3+) |
| Complex domain, many use cases | ❌ Use Handlers + CQRS (API 5) |

## Where to Go Next

- [API 0 — Transaction Script](../migration/api0-transaction-script.md) — The
  migration guide for Api0a → Api0b.
- [Repository Pattern](repository-pattern.md) — The abstraction that
  Transaction Script deliberately avoids.
- [Unit of Work Pattern](unit-of-work-pattern.md) — The transaction
  management that Transaction Script replaces with direct `SaveChangesAsync`.
