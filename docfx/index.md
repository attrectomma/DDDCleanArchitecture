# RetroBoard — Clean Architecture & DDD Learning Guide

Welcome to the **RetroBoard** documentation site. This is an educational resource
that teaches .NET developers how to evolve a codebase from naïve CRUD toward
proper Domain-Driven Design through **five progressive implementations** of the
same business domain.

## Who Is This For?

Developers from **intern to mid/medior level** who want to understand:

- Why "table → entity → repository → service → controller" breaks down
- What Clean Architecture actually looks like in .NET
- How Domain-Driven Design concepts (aggregates, consistency boundaries) solve real problems
- When and why to introduce CQRS and the Mediator pattern

## The Five APIs

Each API implements the same **Retrospective Board** domain with identical REST
endpoints and the same integration test suite — but with progressively more
sophisticated architecture:

| API | Theme | Key Concept |
|-----|-------|-------------|
| [API 1 — Anemic CRUD](migration/api1-anemic-crud.md) | The junior baseline | Services own all logic, entities are property bags |
| [API 2 — Rich Domain](migration/api2-rich-domain.md) | Push logic into entities | Private setters, guard methods, domain exceptions |
| [API 3 — Aggregates](migration/api3-aggregates.md) | Consistency boundaries | Aggregate roots, optimistic concurrency, fewer repositories |
| [API 4 — Split Aggregates](migration/api4-split-aggregates.md) | Right-size your aggregates | Vote as its own aggregate, cross-aggregate checks |
| [API 5 — CQRS + MediatR](migration/api5-cqrs-mediatr.md) | Behavior over nouns | Command/Query separation, domain events, pipeline behaviors |

## Quick Navigation

- **[Core Concepts](concepts/index.md)** — Learn the building blocks (entities, repositories, services, DTOs, aggregates, etc.)
- **[Migration Path](migration/index.md)** — Walk through each API tier and understand what changes and why
- **[Architecture](architecture/index.md)** — Clean Architecture layers, dependency rules, and project structure
- **[Patterns](patterns/index.md)** — Design patterns used in this repository (Unit of Work, Repository, CQRS, Mediator)
- **[Testing](testing/index.md)** — Integration test strategy, Testcontainers, Respawn, shared test infrastructure

## Getting Started

```bash
# Clone and run
git clone https://github.com/attrectomma/DDDCleanArchitecture.git
cd DDDCleanArchitecture

# Start PostgreSQL
docker compose up -d

# Run any API
dotnet run --project src/Api1.AnemicCrud/Api1.WebApi

# Run all integration tests
dotnet test
```
