# DDD & Clean Architecture — A Progressive Learning Repository

> **Compare five implementations of the same business domain, each showcasing a different level of architectural maturity — from naïve CRUD to full Domain-Driven Design with the Mediator pattern.**

---

## 🎯 Purpose

This repository is **educational material** aimed at developers from intern level to mid/medior level. It answers the question:

*"What does it actually look like when you evolve a codebase from simple CRUD toward Clean Architecture and Domain-Driven Design?"*

Rather than describing these concepts in theory, we build the **exact same Retrospective Board application** five times — each time applying a more sophisticated design approach. Every API has the same REST contract and is verified by the same integration test suite, making side-by-side comparison straightforward.

---

## 📋 The Domain

We model a **Retrospective Board** tool (think: a simplified retro app for agile teams).

| Concept | Description |
|---------|-------------|
| **User** | A person who can be assigned to projects |
| **Project** | Groups users together; a project can have multiple retro boards |
| **RetroBoard** | A single retrospective session with columns |
| **Column** | A category on the board (e.g., "What went well", "Action items") |
| **Note** | A sticky note placed in a column |
| **Vote** | A user's vote on a note |

### Business Rules (Invariants)

- Column names within a retro board must be **unique**.
- Note text within a column must be **unique**.
- A user may cast only **one vote per note**.
- Only users assigned to a project may participate in its retros.

---

## 🏗️ The Five APIs

Each API implements the same domain and exposes the same REST endpoints, but with progressively better architecture:

### API 1 — Anemic CRUD
> *"This is what you frequently encounter from juniors."*

- **Pattern:** Table → Entity → Repository → Service → Controller (1-to-1)
- **Business logic lives in:** Service layer
- **Domain models:** Anemic (property bags with public setters, no behavior)
- **Concurrency:** None — last write wins silently
- **Key lesson:** This works for small apps but scatters business rules across multiple services and has no protection against race conditions.

### API 2 — Rich Domain Models
> *"Same structure, but push logic into the entities."*

- **Pattern:** Same Clean Architecture layers as API 1
- **Business logic lives in:** Domain entities (private setters, guard methods, factory constructors)
- **Concurrency:** Still none
- **Key lesson:** Rich domain models centralize invariant enforcement, but without aggregate boundaries the consistency model is still fragile.

### API 3 — Aggregate Design
> *"Introduce consistency boundaries."*

- **Pattern:** Two aggregates — **Project** (owns members) and **RetroBoard** (owns columns → notes → votes)
- **Business logic lives in:** Aggregate roots
- **Concurrency:** Optimistic locking via PostgreSQL's `xmin` column
- **Repositories/Services:** Per-aggregate (not per-entity) — dramatically fewer classes
- **Key lesson:** Aggregates provide a clear consistency boundary, but a large aggregate (RetroBoard with all its children) leads to write contention and expensive loads.

### API 4 — Split Aggregates
> *"Extract Vote as its own aggregate to reduce contention."*

- **Pattern:** Three aggregates — Project, RetroBoard (columns + notes), and **Vote** (standalone)
- **Business logic lives in:** Aggregate roots + cross-aggregate application checks
- **Concurrency:** Optimistic locking per aggregate; DB unique constraints as safety nets
- **Key lesson:** Smaller aggregates improve write scalability but introduce the need for cross-aggregate invariant enforcement and eventual consistency trade-offs.

### API 5 — Behavior-Centric + CQRS + MediatR
> *"Stop thinking in nouns. Think in behaviors. Separate reads from writes."*

- **Pattern:** Commands, Queries, and Domain Events via **MediatR**; **CQRS** separates read and write paths
- **Business logic lives in:** Command handlers + aggregate roots
- **Reads:** Query handlers project directly from the database (no aggregate loading, no change tracking)
- **Writes:** Command handlers load aggregates through repositories, enforce invariants, and persist via UoW
- **Cross-cutting concerns:** Pipeline behaviors (validation, logging, transactions)
- **Domain Events:** Aggregates raise events; handlers react (decoupled side effects)
- **Voting strategies:** **Strategy pattern** + **Specification pattern** enable configurable voting rules (Default: one vote per note; Budget: dot voting with per-column limits). Strategies compose different specifications using AND/OR/NOT boolean algebra.
- **Key lesson:** CQRS eliminates the waste of loading full aggregates for read-only requests. Behavior-centric design scales better for larger teams and feature sets, but adds indirection and a steeper learning curve. The Strategy + Specification patterns demonstrate how to make business rules configurable without modifying existing code. This is "CQRS lite" — same database, separated code paths.

---

## 📊 Tier Comparison

| Aspect | API 1 | API 2 | API 3 | API 4 | API 5 |
|--------|:-----:|:-----:|:-----:|:-----:|:-----:|
| Business logic location | Services | Entities | Aggregate roots | Aggregate roots | Handlers + Domain |
| Repository granularity | Per-table | Per-table | Per-aggregate | Per-aggregate | Per-aggregate |
| Consistency boundary | ❌ None | ❌ None | ✅ Aggregate | ✅ Aggregate | ✅ Aggregate |
| Optimistic concurrency | ❌ | ❌ | ✅ | ✅ | ✅ |
| Write contention risk | N/A | N/A | ⚠️ High | ✅ Low | ✅ Low |
| CQRS (read/write split) | ❌ | ❌ | ❌ | ❌ | ✅ |
| Mediator pattern | ❌ | ❌ | ❌ | ❌ | ✅ |
| Domain events | ❌ | ❌ | ❌ | ❌ | ✅ |
| Strategy + Specification | ❌ | ❌ | ❌ | ❌ | ✅ |

---

## 🧪 Testing Strategy

The repository uses a **two-layer testing strategy**:

### Domain Unit Tests (API 2–5)

Rich domain entities and aggregate roots are **unit testable without any infrastructure** — no Docker, no database, no HTTP, no mocking. Each test constructs an entity in-memory, calls a method, and asserts the outcome. API 1 is excluded because its anemic entities have no behavior to test.

| Project | What It Tests | Test Count |
|---------|--------------|------------|
| `Api2.Domain.UnitTests` | Entity constructors, guards, invariant methods | ~29 |
| `Api3.Domain.UnitTests` | Aggregate root operations (column/note/vote through RetroBoard) | ~25 |
| `Api4.Domain.UnitTests` | Same as API 3, minus vote (Vote is its own aggregate) | ~23 |
| `Api5.Domain.UnitTests` | Same as API 4, plus domain event assertions + Strategy/Specification tests | ~76 |

### Integration Tests (All 5 APIs)

All five APIs share the **exact same integration test suite**. Tests run end-to-end: HTTP request → API → PostgreSQL (running in Docker via Testcontainers).

| Test Category | API 1 | API 2 | API 3 | API 4 | API 5 |
|--------------|:-----:|:-----:|:-----:|:-----:|:-----:|
| CRUD happy path | ✅ | ✅ | ✅ | ✅ | ✅ |
| Invariant enforcement | ✅ | ✅ | ✅ | ✅ | ✅ |
| Soft delete | ✅ | ✅ | ✅ | ✅ | ✅ |
| Concurrency conflicts | ❌ Fail | ❌ Fail | ✅ Pass | ✅ Pass | ✅ Pass |
| Consistency under load | ❌ Fail | ❌ Fail | ✅ Pass | ✅ Pass | ✅ Pass |

> Concurrency tests are **designed to fail** on API 1 and API 2. This is the point — it makes the value of proper aggregate design tangible.

---

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 8 (LTS) |
| Web Framework | ASP.NET Core |
| ORM | EF Core 8 + Npgsql |
| Database | PostgreSQL 16 (Docker) |
| Validation | FluentValidation |
| Mediator (API 5) | MediatR 12 |
| Testing | xUnit, Testcontainers, Respawn, FluentAssertions |

---

## 📂 Repository Structure

```
├── docs/                          # Implementation plans and design decision docs
│   ├── DesignDecisions.md         # Cross-API design decisions comparison
│   └── 01–05 per-API plans        # Detailed plans for each API tier
├── docfx/                         # DocFX documentation site (deployed to GitHub Pages)
│   ├── concepts/                  # Core concept explanations
│   ├── migration/                 # Per-API tier migration guides
│   ├── architecture/              # Layer descriptions and dependency rules
│   ├── patterns/                  # Design pattern explanations
│   ├── testing/                   # Test strategy documentation
│   └── api/                       # Auto-generated API reference
├── src/
│   ├── Api1.AnemicCrud/           # API 1 — Anemic CRUD
│   ├── Api2.RichDomain/           # API 2 — Rich Domain Models
│   ├── Api3.Aggregates/           # API 3 — Aggregate Design
│   ├── Api4.SplitAggregates/      # API 4 — Vote as separate aggregate
│   └── Api5.Behavioral/           # API 5 — MediatR + Domain Events
├── tests/
│   ├── RetroBoard.IntegrationTests.Shared/
│   ├── Api1.IntegrationTests/
│   ├── Api2.IntegrationTests/
│   ├── Api3.IntegrationTests/
│   ├── Api4.IntegrationTests/
│   ├── Api5.IntegrationTests/
│   ├── Api2.Domain.UnitTests/         # Domain unit tests (API 2–5)
│   ├── Api3.Domain.UnitTests/
│   ├── Api4.Domain.UnitTests/
│   └── Api5.Domain.UnitTests/
├── .github/workflows/docs.yml     # GitHub Actions: build & deploy DocFX to Pages
├── docker-compose.yml
└── RetroBoard.sln
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started) (for PostgreSQL)

### Run an API
```bash
# Start PostgreSQL
docker compose up -d

# Run API 1 (or any API)
dotnet run --project src/Api1.AnemicCrud/Api1.WebApi
```

### Run Tests
```bash
# Docker must be running — Testcontainers will spin up a Postgres instance
dotnet test
```

---

## 📖 Documentation

**📘 [View the full documentation site →](https://attrectomma.github.io/DDDCleanArchitecture/)**

The DocFX-powered documentation site includes:
- **[Core Concepts](https://attrectomma.github.io/DDDCleanArchitecture/concepts/)** — Entities, aggregates, repositories, DTOs, and more
- **[Migration Path](https://attrectomma.github.io/DDDCleanArchitecture/migration/)** — Walk through each API tier and understand what changes and why
- **[Architecture](https://attrectomma.github.io/DDDCleanArchitecture/architecture/)** — Clean Architecture layers, dependency rules, project structure
- **[Design Patterns](https://attrectomma.github.io/DDDCleanArchitecture/patterns/)** — Repository, Unit of Work, CQRS, Mediator, Domain Events, Interceptors, Specification, Strategy
- **[Testing Strategy](https://attrectomma.github.io/DDDCleanArchitecture/testing/)** — Integration tests, Testcontainers, Respawn, shared test infrastructure
- **[API Reference](https://attrectomma.github.io/DDDCleanArchitecture/api/)** — Auto-generated from XML doc comments in the source code
- **[Design Decisions](docs/DesignDecisions.md)** — Cross-API comparison of all key architectural decisions

Code is heavily commented using .NET XML doc comments and `// DESIGN:` annotations that reference cross-tier comparisons.

---

## 🤝 Contributing

This is an educational project. If you spot mistakes, have suggestions for better examples, or want to propose improvements to the teaching narrative, feel free to open an issue or PR.

---

## 📄 License

[MIT](LICENSE)
