# Dependency Rules

## The Fundamental Rule

> Inner layers must never depend on outer layers.

```
WebApi  →  Application  →  Domain  ←  Infrastructure
```

- **Domain** depends on **nothing** (no NuGet packages beyond the base framework).
- **Application** depends on **Domain**.
- **Infrastructure** depends on **Domain** (implements repository interfaces).
- **WebApi** depends on **Application** (and transitively on Domain).
- **WebApi** also references **Infrastructure** — but only for DI registration
  in `Program.cs`.

## How It Works in Practice

### Repository Interface (Domain Layer)

```csharp
// Defined in Api3.Domain — no EF Core references
namespace Api3.Domain.RetroAggregate;

public interface IRetroBoardRepository
{
    Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RetroBoard board, CancellationToken ct = default);
    void Delete(RetroBoard board);
}
```

### Repository Implementation (Infrastructure Layer)

```csharp
// Defined in Api3.Infrastructure — uses EF Core
namespace Api3.Infrastructure.Persistence.Repositories;

public class RetroBoardRepository : IRetroBoardRepository
{
    private readonly RetroBoardDbContext _context;

    public async Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
}
```

### DI Wiring (WebApi Layer)

```csharp
// Program.cs — the only place Infrastructure is referenced from WebApi
builder.Services.AddScoped<IRetroBoardRepository, RetroBoardRepository>();
```

## The Dependency Inversion Principle

The key insight: **the Domain layer defines what it needs** (via interfaces),
and **the Infrastructure layer provides it** (via implementations). The
Domain never "reaches down" to the database — the database "reaches up" to
satisfy the Domain's contract.

This means you could swap PostgreSQL for SQL Server, MongoDB, or even an
in-memory store by writing new Infrastructure implementations — without
changing a single line in Domain or Application.

## Project Reference Graph

```
Api3.WebApi
  ├── references Api3.Application
  │     └── references Api3.Domain
  └── references Api3.Infrastructure
        └── references Api3.Domain
```

Note: Application does **not** reference Infrastructure. If an Application
service needs to save data, it depends on `IUnitOfWork` (interface in
Application or Domain) — the implementation comes from Infrastructure via DI.
