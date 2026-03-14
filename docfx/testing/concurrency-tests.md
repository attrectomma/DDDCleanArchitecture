# Why Concurrency Tests Fail on API 1 & 2

## The Intentional Failure

The concurrency tests in `ConcurrencyTestsBase` are **designed to fail** on
API 1 and API 2. This is not a bug — it's the core teaching point.

## The Test

```csharp
[Fact]
public async Task AddColumn_ConcurrentDuplicateNames_OnlyOneSucceeds()
{
    // Fire two identical "add column" requests simultaneously
    var tasks = Enumerable.Range(0, 2)
        .Select(_ => client.PostAsJsonAsync(url, new { Name = "Same Name" }))
        .ToArray();

    var responses = await Task.WhenAll(tasks);

    // Assert: exactly one 201, one 409
    responses.Should().ContainSingle(r => r.StatusCode == HttpStatusCode.Created);
    responses.Should().ContainSingle(r => r.StatusCode == HttpStatusCode.Conflict);
}
```

## Why API 1 & 2 Fail

Both requests execute this sequence:

```
Thread A: ExistsByName("Same Name") → false     Thread B: ExistsByName("Same Name") → false
Thread A: INSERT column                         Thread B: INSERT column
Thread A: SaveChanges ✅                        Thread B: SaveChanges ✅
```

Both pass the check because neither has saved yet when the other checks. This
is the **check-then-act race condition**.

Result: **two columns with the same name** — invariant violated.

## Why API 3+ Pass

API 3 uses optimistic concurrency on the aggregate root:

```
Thread A: Load RetroBoard (xmin=100)            Thread B: Load RetroBoard (xmin=100)
Thread A: AddColumn("Same Name") → OK           Thread B: AddColumn("Same Name") → OK
Thread A: SaveChanges WHERE xmin=100 → ✅       Thread B: SaveChanges WHERE xmin=100 → ❌
                        (xmin becomes 101)                  (xmin is 101, not 100!)
                                                            → DbUpdateConcurrencyException
                                                            → 409 Conflict
```

The second save detects that the aggregate was modified since it was loaded.

## The Teaching Moment

This demonstrates that:
1. **Application-level checks are not sufficient** for concurrent environments.
2. **Optimistic concurrency** (or database-level constraints) is needed.
3. **Aggregate design** naturally provides the mechanism for this.

Students can run the tests against each API and **see the difference** in
test results — making the abstract concept tangible.

## API 5 — Conditional Constraints via the Options Pattern

API 5 introduces configurable voting strategies (Default vs Budget). This
creates a new dimension in concurrency testing: the **database schema itself
depends on configuration**.

### The Problem

In API 4, a **unique index** on `Vote(NoteId, UserId)` served as a DB safety
net against duplicate votes. API 5's Budget strategy allows multiple votes per
note per user ("dot voting"), so the constraint cannot be unconditionally
applied.

### The Solution — Conditional Unique Index

The `RetroBoardDbContext` receives `IOptions<VotingOptions>` and conditionally
applies the unique index in `OnModelCreating`:

```csharp
if (_votingOptions.DefaultVotingStrategy == VotingStrategyType.Default)
{
    modelBuilder.Entity<Vote>(builder =>
    {
        builder.HasIndex(v => new { v.NoteId, v.UserId })
            .HasDatabaseName("IX_Vote_NoteId_UserId")
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");
    });
}
```

When the configured strategy is **Default**, the unique constraint is applied
and the shared `CastVote_ConcurrentDuplicateVotes_OnlyOneSucceeds` test passes
— the DB catches the second concurrent vote. When the strategy is **Budget**,
the index is non-unique and duplicate votes are allowed.

### Separate Test Fixtures

Because the DB schema changes with configuration, API 5 uses **two separate
test fixtures**, each with its own PostgreSQL container:

| Fixture | Strategy | Unique Index | Tests |
|---------|----------|-------------|-------|
| `Api5Fixture` | Default | ✅ Yes | Shared tests (including concurrency) |
| `Api5BudgetFixture` | Budget | ❌ No | Budget-specific voting tests |

```csharp
// Api5Fixture — Default strategy, unique constraint active
services.Configure<VotingOptions>(opts =>
{
    opts.DefaultVotingStrategy = VotingStrategyType.Default;
    opts.MaxVotesPerColumn = 3;
});

// Api5BudgetFixture — Budget strategy, no unique constraint
services.Configure<VotingOptions>(opts =>
{
    opts.DefaultVotingStrategy = VotingStrategyType.Budget;
    opts.MaxVotesPerColumn = 3;
});
```

### EF Core Model Caching

A subtle but critical detail: EF Core caches the compiled model **per context
type**. If two fixtures configure different `VotingOptions`, EF would reuse
the first cached model for both — applying the wrong schema.

A custom `VotingStrategyModelCacheKeyFactory` includes the
`DefaultVotingStrategy` value in the cache key, forcing EF Core to build
separate models per configuration:

```csharp
public class VotingStrategyModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        if (context is RetroBoardDbContext retroContext)
            return (context.GetType(), retroContext.VotingOptions.DefaultVotingStrategy, designTime);

        return (context.GetType(), designTime);
    }
}
```

### Teaching Points

This API 5 testing approach adds three new lessons:

1. **Configuration affects schema** — the Options pattern doesn't just change
   application behavior, it can change the database structure.
2. **Test isolation requires infrastructure isolation** — different schema
   configurations need separate database containers.
3. **EF Core model caching is global** — when the model depends on runtime
   configuration, a custom `IModelCacheKeyFactory` is required.

For more details, see the [Options Pattern](../patterns/options-pattern.md)
documentation.
