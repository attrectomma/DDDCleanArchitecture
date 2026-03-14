# Test Infrastructure

## Two Layers of Testing

This repository uses a **two-layer testing strategy**:

| Layer | What It Tests | Infrastructure Required | Speed |
|-------|--------------|------------------------|-------|
| **Domain unit tests** | Entity constructors, guard clauses, invariant methods, domain events | None — pure in-memory | Milliseconds |
| **Integration tests** | Full HTTP request → API → PostgreSQL round-trips, concurrency, soft delete | Docker (Testcontainers), WebApplicationFactory, Respawn | Seconds |

Domain unit tests exist for API 2–5 (API 1's anemic entities have no behavior
to test). They have **zero infrastructure dependencies** — no Docker, no
database, no HTTP. See [Domain Unit Tests](unit-tests.md) for details.

The rest of this page describes the **integration test** infrastructure.

## Technology Stack

| Tool | Purpose |
|------|---------|
| **xUnit** | Test framework |
| **Testcontainers** | Spins up a PostgreSQL Docker container per test run |
| **WebApplicationFactory** | Hosts the API in-process for HTTP testing |
| **Respawn** | Resets database state between tests (faster than recreate) |
| **FluentAssertions** | Readable assertion syntax (`.Should().Be()`) |

## How It Works

### 1. PostgresFixture (Container Lifecycle)

```csharp
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

Started **once** per test collection. All tests share the same container.

### 2. ApiFixture (WebApplicationFactory Wrapper)

Each API's test project provides a fixture that:
- Creates a `WebApplicationFactory<Program>` with the test DB connection
- Applies EF Core migrations
- Initializes Respawn for fast resets
- Provides an `HttpClient` for making requests

### 3. Respawn (Database Reset)

Instead of recreating the database per test (slow), Respawn truncates all
tables:

```csharp
public async Task ResetDatabaseAsync()
{
    await using var connection = new NpgsqlConnection(ConnectionString);
    await connection.OpenAsync();
    await _respawner.ResetAsync(connection);
}
```

Called in each test class's `InitializeAsync` — ensures test isolation
without the cost of migration replay.

## Prerequisites

- **Docker must be running.** Testcontainers starts a Postgres container
  automatically — no pre-configured database needed.
- **.NET 8 SDK** installed.
