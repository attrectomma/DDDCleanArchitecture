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
