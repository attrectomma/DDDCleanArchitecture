# Overall Implementation Plan

## 1. Goal

Build five progressively more sophisticated .NET Web APIs implementing the same **Retrospective Board** domain, showcasing the journey from naïve "table-per-controller" architecture to proper Aggregate Design and DDD. Every API shares the **exact same integration-test suite** running against a Postgres container.

---

## 2. Solution & Repository Layout

```
RetroBoard.sln
│
├── docs/
│   ├── 00-OverallImplementationPlan.md          (this file)
│   ├── 01-Api1-DetailedPlan.md
│   ├── 02-Api2-DetailedPlan.md
│   ├── 03-Api3-DetailedPlan.md
│   ├── 04-Api4-DetailedPlan.md
│   ├── 05-Api5-DetailedPlan.md
│   └── DesignDecisions.md                       (cross-API comparison)
│
├── src/
│   ├── Api1.AnемicCrud/                          # API 1 – Anemic CRUD
│   │   ├── Api1.Domain/                          # Anemic entities, AuditableEntityBase
│   │   ├── Api1.Application/                     # Service interfaces & implementations
│   │   ├── Api1.Infrastructure/                  # EF Core DbContext, repos, interceptors
│   │   └── Api1.WebApi/                          # Controllers, Program.cs
│   │
│   ├── Api2.RichDomain/                          # API 2 – Rich Domain Models
│   │   ├── Api2.Domain/
│   │   ├── Api2.Application/
│   │   ├── Api2.Infrastructure/
│   │   └── Api2.WebApi/
│   │
│   ├── Api3.Aggregates/                          # API 3 – Aggregate Design (Project + Retro aggs)
│   │   ├── Api3.Domain/
│   │   ├── Api3.Application/
│   │   ├── Api3.Infrastructure/
│   │   └── Api3.WebApi/
│   │
│   ├── Api4.SplitAggregates/                     # API 4 – Vote extracted as own aggregate
│   │   ├── Api4.Domain/
│   │   ├── Api4.Application/
│   │   ├── Api4.Infrastructure/
│   │   └── Api4.WebApi/
│   │
│   └── Api5.Behavioral/                          # API 5 – Behavior-centric + MediatR
│       ├── Api5.Domain/
│       ├── Api5.Application/
│       ├── Api5.Infrastructure/
│       └── Api5.WebApi/
│
├── tests/
│   ├── RetroBoard.IntegrationTests.Shared/       # Shared test fixtures, helpers, Testcontainers setup
│   ├── Api1.IntegrationTests/
│   ├── Api2.IntegrationTests/
│   ├── Api3.IntegrationTests/
│   ├── Api4.IntegrationTests/
│   └── Api5.IntegrationTests/
│
├── docker-compose.yml                            # Postgres for local dev
├── Directory.Build.props                         # Central package versions, common settings
├── .editorconfig
└── .gitignore
```

---

## 3. Domain Model (shared concept, API-specific implementations)

### Entities

| Entity       | Key Properties                                                      |
|-------------|----------------------------------------------------------------------|
| **User**     | Id, Name, Email                                                      |
| **Project**  | Id, Name, Members (→ User)                                           |
| **RetroBoard** | Id, ProjectId, Name, Columns                                       |
| **Column**   | Id, RetroBoardId, Name, Notes                                        |
| **Note**     | Id, ColumnId, Text, Votes                                            |
| **Vote**     | Id, NoteId, UserId                                                   |

### Invariants

1. Column names within a RetroBoard must be **unique**.
2. Note text within a Column must be **unique**.
3. A User may cast only **1 vote per Note**.
4. Only Users assigned to the Project may participate in its Retros.

### AuditableEntityBase

All entities inherit from:

```csharp
public abstract class AuditableEntityBase
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }   // soft delete
    public bool IsDeleted => DeletedAt.HasValue;
}
```

Timestamps are set via an **EF Core `SaveChangesInterceptor`**.

---

## 4. Cross-Cutting Concerns (shared across all APIs)

| Concern | Implementation |
|---------|---------------|
| **Auditing** | `AuditableEntityBase` + `AuditInterceptor : SaveChangesInterceptor` |
| **Soft Delete** | Global query filter `entity.DeletedAt == null`; override in interceptor |
| **Unit of Work** | `IUnitOfWork` wrapping `DbContext.SaveChangesAsync()` |
| **Database** | PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL` |
| **Concurrency** | `xmin` system column as concurrency token (Postgres-native) |
| **Mapping** | DTOs ↔ Entities via manual mapping (no AutoMapper, keeps it explicit for teaching) |
| **Validation** | FluentValidation on incoming DTOs |
| **Error Handling** | Global exception-handling middleware returning Problem Details (RFC 7807) |
| **API Docs** | Swagger / OpenAPI via Swashbuckle |

---

## 5. Integration Test Strategy

### Infrastructure

- **Testcontainers for .NET** — spins up a Postgres container per test run.
- **WebApplicationFactory<Program>** — hosts each API in-process.
- **Respawn** — resets DB state between tests (faster than recreate).

### Shared Test Project (`RetroBoard.IntegrationTests.Shared`)

Contains:
- `PostgresFixture` — manages Testcontainers lifecycle.
- `ApiFixture<TProgram>` — wraps `WebApplicationFactory`, applies migrations, seeds data.
- Base test classes and helper extension methods (`HttpClient` helpers, assertion helpers).
- **Shared test cases as abstract classes** — each API test project inherits them and only provides the `WebApplicationFactory`.

### Test Categories

| Category | Description | Expected Pass |
|----------|------------|---------------|
| **CRUD Happy Path** | Create / Read / Update / Delete for every resource | All APIs |
| **Invariant Enforcement** | Duplicate column names, duplicate notes, double-voting | All APIs |
| **Soft Delete** | Deleted entities excluded from queries, can be restored | All APIs |
| **Concurrency** | Optimistic concurrency conflicts on simultaneous writes | ❌ API 1 & 2, ✅ API 3 & 4 |
| **Consistency Boundary** | Race conditions on aggregate state | ❌ API 1 & 2, ✅ API 3 & 4 |

---

## 6. Endpoint Contract (identical across all APIs)

All APIs expose the same REST surface so tests are reusable:

```
# Users
POST   /api/users
GET    /api/users/{id}

# Projects
POST   /api/projects
GET    /api/projects/{id}
POST   /api/projects/{id}/members          (assign user)
DELETE /api/projects/{id}/members/{userId}  (remove user)

# Retro Boards
POST   /api/projects/{projectId}/retros
GET    /api/projects/{projectId}/retros/{retroId}

# Columns
POST   /api/retros/{retroId}/columns
PUT    /api/retros/{retroId}/columns/{columnId}
DELETE /api/retros/{retroId}/columns/{columnId}

# Notes
POST   /api/columns/{columnId}/notes
PUT    /api/columns/{columnId}/notes/{noteId}
DELETE /api/columns/{columnId}/notes/{noteId}

# Votes
POST   /api/notes/{noteId}/votes            (body: { userId })
DELETE /api/notes/{noteId}/votes/{voteId}
```

> API 3+ may nest routes differently internally, but the external contract stays the same.

---

## 7. Technology Stack

| Component | Package / Version |
|-----------|------------------|
| Runtime | .NET 8 (LTS) |
| Web Framework | ASP.NET Core Minimal Hosting (`WebApplication.CreateBuilder`) |
| ORM | EF Core 8 + Npgsql provider |
| Validation | FluentValidation.AspNetCore |
| Mediator (API 5) | MediatR 12 |
| Testing | xUnit, Testcontainers, Respawn, FluentAssertions |
| Containers | Docker Compose (Postgres 16) |

---

## 8. Build Order / Phases

| Phase | Deliverable |
|-------|-------------|
| **Phase 0** | Solution scaffold, `Directory.Build.props`, docker-compose, `.gitignore`, shared test infra |
| **Phase 1** | API 1 — Anemic CRUD (baseline) + full integration tests |
| **Phase 2** | API 2 — Rich Domain (refactor business logic into entities) |
| **Phase 3** | API 3 — Aggregate Design (Project + Retro aggregates, consistency) |
| **Phase 4** | API 4 — Split Vote aggregate, cross-aggregate checks |
| **Phase 5** | API 5 — Behavior-centric + CQRS + MediatR |
| **Phase 6** | Documentation: DesignDecisions.md, per-API `.md` comparisons |

---

## 9. Naming & Coding Conventions

- **Namespaces** mirror folder structure: `Api1.Domain.Entities`, `Api1.Application.Services`, etc.
- **XML doc comments** on all public types and members.
- **`// DESIGN:` comments** explain *why* a design choice was made and reference tier comparisons.
- Separate `Requests/` and `Responses/` DTO folders per API.
- One class per file.

---

## 10. Key Design Decisions Across Tiers

| Decision | API 1 | API 2 | API 3 | API 4 | API 5 |
|----------|-------|-------|-------|-------|-------|
| Business logic location | Service layer | Domain entities | Aggregate roots | Aggregate roots | Command handlers + domain |
| Repository granularity | Per-table | Per-table | Per-aggregate | Per-aggregate (2 aggs) | Per-aggregate (writes only) |
| Consistency boundary | None (hope & pray) | None | Aggregate = txn boundary | Smaller aggregates | Aggregate + domain events |
| Concurrency handling | None | None | Optimistic (version/xmin) | Optimistic | Optimistic |
| Write contention risk | N/A | N/A | High (big Retro agg) | Low (Vote split out) | Low |
| Read/Write separation (CQRS) | No | No | No | No | Yes — queries bypass aggregates |
| Mediator | No | No | No | No | Yes (MediatR) |
