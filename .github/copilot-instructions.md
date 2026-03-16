# Copilot Instructions — .NET Best Practices for AI-Assisted Engineering

This file defines rules and conventions for AI code generation in this repository.
These instructions apply to GitHub Copilot, Copilot Chat, and any AI-assisted
development tooling used by contributors.

---

## Project Context

This is an educational .NET 8 repository with five Web APIs demonstrating
progressive architectural maturity (Anemic CRUD → DDD + CQRS). Each API has
its own set of four projects (Domain, Application, Infrastructure, WebApi)
and a corresponding integration test project. All APIs share a common
integration test base in `tests/RetroBoard.IntegrationTests.Shared/`.

---

## General Rules

1. **Target .NET 8 (LTS).** Do not use preview features or APIs from .NET 9+.
2. **Follow the existing project structure.** Each API has four layers: Domain, Application, Infrastructure, WebApi. Do not merge layers or add new ones unless the plan documents explicitly call for it.
3. **One class per file.** File name must match the class/record/interface name.
4. **Use file-scoped namespaces** (`namespace X;` not `namespace X { }`).
5. **Use `Guid` for all entity IDs.** Never use `int` or `long` for primary keys.
6. **Nullable reference types are enabled.** Never suppress nullable warnings with `!` unless there is a documented reason. Prefer null checks and guard clauses.
7. **Do not use `var` for non-obvious types.** Use explicit types when the right-hand side does not clearly indicate the type.
8. **Prefer `async/await` over `.Result` or `.Wait()`.** Never block on async code.
9. **Use `CancellationToken`** on all async method signatures and pass it through the entire call chain.

---

## Architecture Rules

### Domain Layer
- Entities must inherit from `AuditableEntityBase`.
- Domain entities must **never** reference infrastructure concerns (no EF Core attributes, no `DbContext`).
- Use **private setters** and constructors for entity properties (except in API 1, which is intentionally anemic).
- Domain exceptions (e.g., `InvariantViolationException`) live in the Domain layer.
- Repository **interfaces** are defined in the Domain layer (API 3+). Implementations are in Infrastructure.

### Application Layer
- Services (API 1–4) or Command/Query handlers (API 5) live here.
- DTOs (Requests/Responses) live here, separated into `Requests/` and `Responses/` folders.
- Validators (FluentValidation) live here.
- The Application layer depends on Domain but **never** on Infrastructure or WebApi.

### Infrastructure Layer
- EF Core `DbContext`, entity configurations (`IEntityTypeConfiguration<T>`), repositories, interceptors, and `UnitOfWork` live here.
- Use **Fluent API** for all EF Core configuration. Do not use data annotations on entities.
- All entities must have a global query filter for soft delete: `.HasQueryFilter(e => e.DeletedAt == null)`.
- Apply unique indexes via Fluent API for all business uniqueness constraints.
- Use `UseXminAsConcurrencyToken()` for aggregate roots in API 3+.

### WebApi Layer
- Controllers must be thin — delegate to services (API 1–4) or `IMediator` (API 5).
- Return Problem Details (RFC 7807) for all error responses.
- Use `[ApiController]` and `[Route]` attributes. Use typed route parameters (e.g., `{id:guid}`).

---

## Code Comments and Documentation

This is an **educational repository**. Comments are critical.

1. **All public types and members must have XML doc comments** (`<summary>`, `<param>`, `<returns>`, `<remarks>`, `<exception>`).
2. **Use `// DESIGN:` comments** to explain architectural decisions, trade-offs, and cross-tier comparisons.
3. **Use `// DESIGN (CQRS foreshadowing):` comments** in API 3/4 to hint at CQRS benefits that API 5 introduces.
4. When generating code for a specific API tier, always include a remark explaining **why this tier does it this way** and **what higher/lower tiers do differently**.
5. Do not add trivial comments (e.g., `// increment counter` above `counter++`).

---

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Projects | `Api{N}.{Layer}` | `Api1.Domain`, `Api3.Infrastructure` |
| Unit test projects | `Api{N}.Domain.UnitTests` | `Api2.Domain.UnitTests` |
| Namespaces | Mirror folder path | `Api1.Domain.Entities` |
| Unit test namespaces | Flat project name | `Api2.Domain.UnitTests` |
| Interfaces | `I` prefix | `IColumnRepository`, `IUnitOfWork` |
| DTOs (requests) | `{Action}{Entity}Request` | `CreateColumnRequest` |
| DTOs (responses) | `{Entity}Response` | `ColumnResponse` |
| Commands (API 5) | `{Verb}{Noun}Command` : `ICommand<TResponse>` | `CastVoteCommand` |
| Queries (API 5) | `Get{Noun}Query` : `IRequest<TResponse>` | `GetRetroBoardQuery` |
| Handlers (API 5) | `{CommandOrQuery}Handler` | `CastVoteCommandHandler` |
| Behaviors (API 5) | `{Concern}Behavior` | `TransactionBehavior`, `ValidationBehavior` |
| Validators | `{RequestOrCommand}Validator` | `CreateColumnRequestValidator` |
| EF Configs | `{Entity}Configuration` | `ColumnConfiguration` |
| Tests | `{Behavior}_When{Condition}_{ExpectedResult}` or descriptive | `AddColumn_WithDuplicateName_ReturnsConflict` |

---

## Testing Rules

### Integration Tests
1. Shared test base classes live in `RetroBoard.IntegrationTests.Shared/Tests/`.
2. API-specific test projects inherit from the shared base classes and only provide fixture wiring.
3. Use **Testcontainers** for PostgreSQL — never depend on a running database outside of Docker.
4. Use **Respawn** to reset DB state between tests — never recreate the database.
5. Use **FluentAssertions** for all assertions (`.Should().Be()`, not `Assert.Equal()`).
6. Tests must be independent and parallelizable. Each test resets the database in its `InitializeAsync`.

### Domain Unit Tests
7. Domain unit tests exist for **API 2–5** (API 1's anemic entities have no behavior to test).
8. Unit test projects are named `Api{N}.Domain.UnitTests` and live in the `tests/` folder.
9. Unit test namespaces are flat: `Api{N}.Domain.UnitTests` — no sub-namespaces.
10. Unit tests require **no infrastructure** — no Docker, no database, no HTTP, no mocking frameworks.
11. Unit tests use **xUnit** (`[Fact]`) and **FluentAssertions**.
12. Each test class covers one entity or aggregate root: `UserTests`, `ProjectTests`, `RetroBoardTests`, etc.
13. Test naming follows: `{Method}_{Condition}_{ExpectedResult}` — e.g., `AddColumn_WithDuplicateName_ThrowsInvariantViolation`.
14. API 5 domain event tests assert the `DomainEvents` collection contains the correct event type and payload.

---

## EF Core and Database Rules

1. Use **PostgreSQL** with `Npgsql.EntityFrameworkCore.PostgreSQL`.
2. Use **code-first migrations**. Migration files are committed to the repository.
3. Use **`DateTime.UtcNow`** for all timestamps. Never use local time.
4. Soft delete via `DeletedAt` timestamp — handled by `AuditInterceptor` (which converts `EntityState.Deleted` → `EntityState.Modified` with `DeletedAt` set).
5. Never call `SaveChanges` from a repository. Only the `UnitOfWork` calls `SaveChangesAsync`.
6. Use `AsNoTracking()` for all read-only queries (especially in API 5 query handlers).

---

## Dependency Injection Rules

1. Register repositories and services as `Scoped`.
2. Register interceptors as `Singleton`.
3. Register pipeline behaviors (API 5) as `Transient`.
4. Use `builder.Services.AddDbContext<T>()` with the interceptor chain.
5. Central package versions are managed in `Directory.Packages.props` — do not add `Version` attributes in `.csproj` files.

---

## Error Handling Rules

1. Domain exceptions → 409 Conflict or 422 Unprocessable Entity.
2. `NotFoundException` → 404 Not Found.
3. Validation failures → 400 Bad Request.
4. `DbUpdateConcurrencyException` → 409 Conflict (API 3+).
5. All error responses use Problem Details format.

---

## Things to Avoid

- ❌ Do not use AutoMapper. Use manual mapping to keep transformations explicit and educational.
- ❌ Do not use `dynamic` or `object` where a specific type is available.
- ❌ Do not add NuGet packages not listed in `Directory.Packages.props` without discussing it first.
- ❌ Do not use `#region` blocks.
- ❌ Do not use `string.Format()` — prefer string interpolation (`$"..."`).
- ❌ Do not catch `Exception` broadly. Catch specific exception types.
- ❌ Do not use `Thread.Sleep()` or any synchronous blocking call.
- ❌ Do not generate code that "works but we'll fix later" — every generated snippet should be production-quality for its tier.

---

## Tier-Specific Reminders

When generating code, check which API tier you are working in:

- **API 0a/0b:** Single project, Transaction Script, Minimal APIs. No layers, no repositories, no services. Business logic is in endpoint handlers. Api0a has no concurrency safety. Api0b adds DB-level concurrency (xmin + unique constraints + middleware). This is intentional — do not add layers.
- **API 1:** Entities are anemic (public setters, no methods). Business logic is in services. This is intentional — do not "improve" it.
- **API 2:** Entities are rich (private setters, methods). Services are thin orchestrators. Some cross-entity checks still live in services — this is intentional.
- **API 3:** Aggregates exist. There are only aggregate-level repositories. No per-entity repositories.
- **API 4:** Vote is its own aggregate. Cross-aggregate checks use DB constraints as safety nets.
- **API 5:** No services — only MediatR command/query handlers. Query handlers bypass repositories and use DbContext directly (CQRS).

---

## Documentation Sync Rules (DocFX)

This repository includes a **DocFX documentation site** in the `docfx/` folder that is deployed to GitHub Pages. The documentation must stay in sync with the code.

1. **Any change to an API's code must be reflected in the corresponding DocFX documentation.** If you modify a pattern, add a new entity, change an invariant, or alter behavior in any API project, update the relevant files in `docfx/`:
   - `docfx/concepts/` — Core concept explanations (entities, repositories, aggregates, etc.)
   - `docfx/migration/` — Per-API tier guides (`api1-anemic-crud.md` through `api5-cqrs-mediatr.md`)
   - `docfx/architecture/` — Layer descriptions, dependency rules, project structure
   - `docfx/patterns/` — Design pattern explanations (Repository, UoW, CQRS, Mediator, Domain Events, Interceptors)
   - `docfx/testing/` — Test infrastructure and strategy documentation

2. **Code examples in documentation must match actual code.** If a code snippet in a `.md` file shows a method signature or class structure, it must reflect the current implementation. Do not leave stale examples.

3. **New concepts or patterns introduced in code must get a documentation page.** If you add a new pattern, cross-cutting concern, or architectural decision, create a new `.md` file in the appropriate `docfx/` subfolder and add it to the section's `toc.yml`.

4. **XML doc comments are the source of truth for API reference.** DocFX generates API reference pages from `<summary>`, `<param>`, `<returns>`, `<remarks>`, and `<exception>` XML doc comments. Keep these complete and accurate.

5. **`// DESIGN:` comments must have a corresponding explanation in `docfx/`.** If you add a `// DESIGN:` or `// DESIGN (CQRS foreshadowing):` comment in code, ensure the concept it references is explained in the DocFX site.

6. **Update the tier comparison tables** in `docfx/migration/index.md` and `README.md` if any metric changes (repository count, handler count, concurrency behavior, etc.).
