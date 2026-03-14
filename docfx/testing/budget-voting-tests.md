# Budget Voting Tests

> **Applies to:** API 5 only.
> Tests the Budget voting strategy end-to-end using a dedicated test fixture
> with its own PostgreSQL container and non-unique Vote index.

## Why a Separate Fixture?

The shared integration tests in `ConcurrencyTestsBase` and other base classes
assume the **Default** voting strategy — one vote per user per note, with a
unique DB constraint as a safety net. The Budget strategy has fundamentally
different rules:

| Aspect | Default (shared tests) | Budget (Api5-specific) |
|--------|----------------------|----------------------|
| Votes per note per user | 1 | Multiple (up to budget) |
| DB unique index on `(NoteId, UserId)` | ✅ Applied | ❌ Not applied |
| Budget limit per column | N/A | `VotingOptions.MaxVotesPerColumn` |

Running the shared concurrency tests against a Budget-configured database would
fail — duplicate votes would succeed instead of conflicting. That's **correct
behavior** for Budget, but breaks the shared test assertions.

The solution: a separate `Api5BudgetFixture` that configures `VotingOptions`
with `VotingStrategyType.Budget` and uses its own Testcontainers PostgreSQL
instance.

## The Budget Fixture

```csharp
public class Api5BudgetFixture : ApiFixture
{
    private readonly PostgresFixture _postgresFixture = new();

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.Configure<VotingOptions>(opts =>
        {
            opts.DefaultVotingStrategy = VotingStrategyType.Budget;
            opts.MaxVotesPerColumn = 3;
        });

        // DbContext registration with Budget-configured VotingOptions...
    }
}
```

Key difference from `Api5Fixture`:
- `DefaultVotingStrategy = VotingStrategyType.Budget` → no unique index
- Own `PostgresFixture` → separate PostgreSQL container (schema isolation)
- Registers `VotingStrategyModelCacheKeyFactory` → prevents EF Core model
  cache conflicts with `Api5Fixture`

## The Tests

`Api5BudgetVotingTests` validates four scenarios:

### 1. Create RetroBoard with Budget Strategy

Verifies that creating a retro board with `VotingStrategyType.Budget`
persists correctly and the strategy is returned in the response.

### 2. Cast Multiple Votes on Same Note (Budget Allows It)

The core differentiator: two votes from the same user on the same note both
succeed with `201 Created`. Under the Default strategy, the second would be
rejected.

```csharp
[Fact]
public async Task CastVote_SameUserSameNote_BudgetAllowsMultiple()
{
    // ... setup board, column, note ...

    HttpResponseMessage first = await client.PostAsJsonAsync(voteUrl, votePayload);
    HttpResponseMessage second = await client.PostAsJsonAsync(voteUrl, votePayload);

    first.StatusCode.Should().Be(HttpStatusCode.Created);
    second.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### 3. Exceed Budget Limit

Casts `MaxVotesPerColumn + 1` votes in the same column. The last vote returns
`409 Conflict` because the `VoteBudgetNotExceededSpecification` rejects it.

### 4. Change Voting Strategy

Verifies that a retro board's strategy can be changed from Budget to Default
via the `ChangeVotingStrategy` endpoint.

## How Options Make This Testable

The same `IOptions<VotingOptions>` mechanism used in production (bound from
`appsettings.json`) is overridden in the fixture with a lambda:

```csharp
services.Configure<VotingOptions>(opts => { ... });
```

This means:
- Tests exercise the **real options pipeline** (no mocks)
- Each fixture controls its own configuration independently
- The conditional unique index in `RetroBoardDbContext.OnModelCreating`
  responds to the configured strategy

## Relationship to Other Tests

```
┌─────────────────────────────────────────────────────┐
│  Shared tests (RetroBoard.IntegrationTests.Shared)  │
│  - ConcurrencyTestsBase                             │
│  - BoardTestsBase, ColumnTestsBase, etc.            │
└────────────────────┬────────────────────────────────┘
                     │ inherited by
          ┌──────────┴──────────┐
          ▼                     │
┌─────────────────┐             │
│  Api5Fixture    │             │
│  (Default)      │             │
│  22 shared      │             │
│  tests          │             │
└─────────────────┘             │
                                │ NOT inherited (separate)
                                ▼
                     ┌─────────────────────┐
                     │  Api5BudgetFixture  │
                     │  (Budget)           │
                     │  4 Api5-specific    │
                     │  tests              │
                     └─────────────────────┘
```

The Budget tests are **not** shared base tests — they're Api5-specific because
no other API tier supports configurable voting strategies.

## See Also

- [Options Pattern](../patterns/options-pattern.md) — How `VotingOptions` is
  configured, validated, and consumed
- [Strategy Pattern](../patterns/strategy-pattern.md) — How voting strategies
  compose different specifications
- [Why Concurrency Tests Fail on API 1 & 2](concurrency-tests.md) — The
  conditional unique index context
