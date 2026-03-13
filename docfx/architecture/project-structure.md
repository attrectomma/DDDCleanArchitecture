# Project Structure

## Solution Layout

```
RetroBoard.slnx
│
├── src/
│   ├── Api1.AnemicCrud/
│   │   ├── Api1.Domain/
│   │   ├── Api1.Application/
│   │   ├── Api1.Infrastructure/
│   │   └── Api1.WebApi/
│   │
│   ├── Api2.RichDomain/        (same 4-layer structure)
│   ├── Api3.Aggregates/        (same 4-layer structure)
│   ├── Api4.SplitAggregates/   (same 4-layer structure)
│   └── Api5.Behavioral/        (same 4-layer structure)
│
├── tests/
│   ├── RetroBoard.IntegrationTests.Shared/   ← Shared fixtures & base test classes
│   ├── Api1.IntegrationTests/                ← Inherits shared tests
│   ├── Api2.IntegrationTests/
│   ├── Api3.IntegrationTests/
│   ├── Api4.IntegrationTests/
│   └── Api5.IntegrationTests/
│
├── docfx/                       ← This documentation site
├── docs/                        ← Implementation plans and design decisions
├── scripts/                     ← Database init scripts
├── Directory.Build.props        ← Central build properties
├── Directory.Packages.props     ← Central NuGet package versions
├── docker-compose.yml           ← PostgreSQL for local development
└── .github/
    ├── copilot-instructions.md  ← AI-assisted engineering rules
    └── workflows/
        └── docs.yml             ← GitHub Actions: build & deploy docs
```

## Naming Conventions

| Element | Pattern | Example |
|---------|---------|---------|
| API projects | `Api{N}.{Layer}` | `Api3.Infrastructure` |
| Namespaces | Mirror folder path | `Api3.Domain.RetroAggregate` |
| Shared test project | `RetroBoard.IntegrationTests.Shared` | — |
| API test projects | `Api{N}.IntegrationTests` | `Api1.IntegrationTests` |

## Why Five Separate API Folders?

Each API is a **complete, independently runnable** application. They don't
share code at the source level (except the shared test infrastructure).
This is intentional:

- **Side-by-side comparison** — Open API 1's `ColumnService` and API 3's
  `RetroBoardService` side by side to see the difference.
- **No accidental coupling** — Changes in API 3 can't break API 1.
- **Progressive disclosure** — Start reading API 1, then see how each
  subsequent API improves on it.
