# Shared Test Pattern

## The Problem

Five APIs × ~20 tests each = 100 test methods. If each API had its own test
code, we'd maintain 100 nearly-identical test methods.

## The Solution

Abstract base classes in `RetroBoard.IntegrationTests.Shared`:

```
RetroBoard.IntegrationTests.Shared/
├── Tests/
│   ├── CrudTestsBase.cs           ← CRUD happy-path tests
│   ├── InvariantTestsBase.cs      ← Business rule enforcement tests
│   ├── ConcurrencyTestsBase.cs    ← Concurrent write tests
│   └── SoftDeleteTestsBase.cs     ← Soft delete behavior tests
├── Fixtures/
│   ├── PostgresFixture.cs
│   └── IntegrationTestCollection.cs
├── Extensions/
│   └── HttpClientExtensions.cs
└── DTOs/
    └── SharedDTOs.cs
```

Each API-specific test project inherits the base classes:

```csharp
// Api1.IntegrationTests/CrudTests.cs
[Collection(TestCollectionNames.IntegrationTests)]
public class CrudTests : CrudTestsBase<Api1Fixture>
{
    public CrudTests(Api1Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync) { }
}
```

That's it. The API-specific test class has **no test methods** — it inherits
all of them from the shared base class.

## Test-Side DTOs

Tests use their own DTO definitions, not the API's:

```csharp
// Shared test DTOs — NOT referencing any API project
public record CreateColumnRequest(string Name);
public record ColumnResponse(Guid Id, string Name, List<NoteResponse>? Notes);
```

This validates the **actual JSON contract**, not just type compatibility.
If an API changes a property name in its DTO but not in the JSON output,
the test catches the mismatch.
