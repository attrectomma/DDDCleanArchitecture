# Rich Domain Models

## The Idea

A **rich domain model** is the opposite of an anemic one. Entities own their
behavior — they enforce their own invariants, expose meaningful methods, and
hide their internal state behind private setters.

The core insight: **the entity is the best place to put a rule that only
involves its own data.**

## Example: API 2 (Rich Domain)

Compare with the [anemic Column](entities.md) from API 1:

```csharp
// API 2 — Rich entity. Owns its invariants.
public class Column : AuditableEntityBase
{
    private readonly List<Note> _notes = new();

    private Column() { }  // EF Core needs a parameterless constructor

    public Column(Guid retroBoardId, string name)
    {
        RetroBoardId = retroBoardId;
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    public Guid RetroBoardId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<Note> Notes => _notes.AsReadOnly();

    public Note AddNote(string text)
    {
        if (_notes.Any(n => n.Text.Equals(text, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"A note with text '{text}' already exists in this column.");

        var note = new Note(Id, text);
        _notes.Add(note);
        return note;
    }

    public void Rename(string newName)
    {
        Name = Guard.AgainstNullOrWhiteSpace(newName, nameof(newName));
    }
}
```

Key differences from API 1:

| Aspect | Anemic (API 1) | Rich (API 2) |
|--------|---------------|--------------|
| Setters | Public | **Private** |
| Constructor | Default only | **Parameterized** — requires valid data |
| Collection access | `ICollection<T>` (mutable) | **`IReadOnlyCollection<T>`** |
| Invariant enforcement | Service layer | **Inside the entity** |

## What Moves Into the Entity?

**Rules that involve only the entity's own state:**
- "Note text must be unique within this column" → `Column.AddNote()`
- "A user can vote only once on this note" → `Note.CastVote()`
- "A name cannot be null or whitespace" → Constructor guard clause

**Rules that stay in the service:**
- "Column name must be unique within the retro board" — a Column doesn't know
  about its sibling columns. This requires loading the parent or the full set
  of siblings, so it remains in the service (API 2) or moves to the aggregate
  root (API 3+).

## Private Setters and EF Core

EF Core can hydrate entities through private setters and backing fields:

```csharp
// EF Core configuration — tell EF to use the backing field
builder.HasMany(c => c.Notes)
    .WithOne()
    .HasForeignKey(n => n.ColumnId);

builder.Navigation(c => c.Notes)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

This means your domain model stays clean — no compromises for the ORM.

## Guard Clauses

Instead of scattering `if (string.IsNullOrWhiteSpace(...))` checks everywhere,
use a `Guard` helper:

```csharp
public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        return value;
    }
}
```

## Limitations

Rich domain models are better than anemic ones, but they don't solve every
problem:

- **Cross-entity rules** still leak into services (column name uniqueness
  across the retro).
- **No consistency boundary** — there's nothing preventing two concurrent
  requests from loading the same entity and both passing the check.
- **Loading strategy coupling** — `Column.AddNote()` only works if the Notes
  collection was loaded. Forgetting to `Include()` means the check silently
  passes.

These limitations are addressed by [aggregates](aggregates.md) in API 3.

## Testability

One of the strongest benefits of rich domain models: **pure invariant methods
are unit testable without mocking or infrastructure.**

Every entity method is a pure function — it takes arguments, mutates
in-memory state, and either returns a result or throws an exception. Tests
can construct an entity directly, call a method, and assert the outcome.
No Docker, no database, no HTTP client, no mock objects.

```csharp
[Fact]
public void AddNote_WithDuplicateText_ThrowsInvariantViolation()
{
    Column column = new Column(Guid.NewGuid(), "What went well");
    column.AddNote("Great teamwork");

    Action act = () => column.AddNote("Great teamwork");

    act.Should().Throw<InvariantViolationException>();
}
```

This is not possible with API 1's anemic entities — they have no methods to
test. Testability is a direct consequence of moving business logic into
entities. See [Domain Unit Tests](../testing/unit-tests.md) for the full
test inventory.

## Where to Go Next

- [Aggregates & Aggregate Roots](aggregates.md) — How API 3 wraps entities
  into consistency boundaries.
- [Coupling & Dependency Management](coupling.md) — Why the dependency between
  loading strategy and domain logic is a form of coupling.
