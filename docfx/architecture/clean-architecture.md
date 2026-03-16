# Clean Architecture Layers

## The Four Layers

Every API in the main progression (API 1–5) follows the same four-layer
structure. API 0 intentionally breaks this convention — see
[When Four Layers Aren't Needed](#when-four-layers-arent-needed) below.

```
┌──────────────────────────────────────────┐
│              WebApi Layer                │
│    Controllers, Middleware, Program.cs    │
├──────────────────────────────────────────┤
│           Application Layer              │
│   Services / Handlers, DTOs, Validators  │
├──────────────────────────────────────────┤
│         Infrastructure Layer             │
│   DbContext, Repos, Configs, Interceptors│
├──────────────────────────────────────────┤
│             Domain Layer                 │
│   Entities, Aggregates, Interfaces,      │
│   Domain Exceptions, Value Objects       │
└──────────────────────────────────────────┘
```

## What Lives Where

### Domain Layer

The **innermost** layer. Has zero external dependencies.

| Contains | Example |
|----------|---------|
| Entities / Aggregate roots | `RetroBoard.cs`, `Column.cs`, `Note.cs` |
| Base classes | `AuditableEntityBase.cs`, `IAggregateRoot.cs` |
| Domain exceptions | `InvariantViolationException.cs` |
| Repository interfaces (API 3+) | `IRetroBoardRepository.cs` |
| Value objects | (if any) |
| Guard clauses | `Guard.cs` |

The Domain layer defines **what the system is** — the business model and rules.

### Application Layer

The **use case** layer. Orchestrates domain operations.

| Contains | Example |
|----------|---------|
| Services (API 1–4) | `ColumnService.cs`, `RetroBoardService.cs` |
| Command/Query handlers (API 5) | `AddColumnCommandHandler.cs` |
| DTOs (Requests & Responses) | `CreateColumnRequest.cs`, `ColumnResponse.cs` |
| Validators | `CreateColumnRequestValidator.cs` |
| Pipeline behaviors (API 5) | `ValidationBehavior.cs` |
| Application exceptions | `NotFoundException.cs` |
| `IUnitOfWork` interface | `IUnitOfWork.cs` |

The Application layer defines **what the system does** — the use cases.

### Infrastructure Layer

The **implementation** layer. Provides concrete implementations.

| Contains | Example |
|----------|---------|
| EF Core DbContext | `RetroBoardDbContext.cs` |
| Entity configurations | `ColumnConfiguration.cs` |
| Repository implementations | `RetroBoardRepository.cs` |
| Interceptors | `AuditInterceptor.cs` |
| Unit of Work implementation | `UnitOfWork.cs` |
| Migrations | `Migrations/` folder |

The Infrastructure layer defines **how things are done** — databases, ORMs, etc.

### WebApi Layer

The **presentation** layer. Handles HTTP concerns.

| Contains | Example |
|----------|---------|
| Controllers | `RetroBoardsController.cs` |
| Exception middleware | `GlobalExceptionHandlerMiddleware.cs` |
| DI configuration | `Program.cs` |
| App settings | `appsettings.json` |

The WebApi layer defines **how users interact** — HTTP, JSON, REST.

## The Dependency Rule

Dependencies point **inward**. Outer layers depend on inner layers, never
the reverse. See [Dependency Rules](dependency-rules.md) for details.

## When Four Layers Aren't Needed

Not every application needs four layers. API 0 demonstrates the **Transaction
Script** pattern — a single project where endpoint handlers talk directly to
the `DbContext`. There is no Domain, Application, or Infrastructure layer.

This is a valid architectural choice when:

- The domain is simple (few entities, few invariants).
- The team is small (1–3 developers).
- Speed of development matters more than long-term maintainability.
- You don't need unit-testable domain logic.

API 0 exists as a **parallel track** to show what you gain — and what you
give up — by adopting layered architecture. See
[API 0 — Transaction Script](../migration/api0-transaction-script.md) and
[Transaction Script Pattern](../patterns/transaction-script-pattern.md)
for the full comparison.
