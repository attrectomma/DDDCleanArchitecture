# Domain Unit Tests

## Why Unit Tests?

The repository's integration tests spin up a full ASP.NET Core host with a
PostgreSQL Testcontainer. They verify end-to-end correctness but are **slow**
(container startup, HTTP round-trips, database I/O) and **coarse-grained**
(a failing invariant test doesn't tell you whether the problem is in the
entity, the service, or the controller).

Domain unit tests complement integration tests by providing:

| Benefit | Explanation |
|---------|-------------|
| **Speed** | Milliseconds per test — no Docker, no database, no HTTP. |
| **Precision** | Tests exercise a single entity method and assert a single outcome. |
| **Documentation** | Each test reads as a specification of the domain rule it covers. |
| **Safety net for refactoring** | Rename a method, change a guard — the unit test catches the regression before the slower integration suite runs. |
| **Educational value** | Demonstrates that rich domain entities are inherently testable — a key advantage over anemic models. |

## Why Start from API 2?

| Tier | Entity Behavior | Unit Testable? |
|------|----------------|----------------|
| API 1 | Property bags — public setters, no methods | ❌ Nothing to test |
| API 2 | Guard clauses, factory constructors, invariant methods | ✅ |
| API 3 | Aggregate root methods enforcing cross-entity invariants | ✅ |
| API 4 | Same as API 3, with Vote as its own aggregate | ✅ |
| API 5 | Same as API 4, plus domain event assertions | ✅ |

API 1's entities are anemic — they have no business logic, no guard clauses,
and no invariant methods. There is nothing to unit test. This is itself a
teaching point: **anemic models are not unit testable because they have no
behavior.**

## No Mocking Required

Domain entities depend on **nothing** — no `DbContext`, no repositories, no
HTTP clients. Every method under test is a pure function that takes arguments,
mutates in-memory state, and either returns a result or throws an exception.
This is a direct consequence of Clean Architecture's dependency rule: the
Domain layer has zero outward dependencies.

See [Why No Mocking?](why-no-mocking.md) for the full analysis.

## What Is Tested

- **Entity constructors** — valid arguments produce a correctly initialized
  entity; invalid arguments throw `ArgumentException`.
- **Guard clauses** — `Guard.AgainstNullOrWhiteSpace` rejects null/empty/whitespace.
- **Invariant-enforcing methods** — `AddNote`, `CastVote`, `AddMember`,
  `AddColumn`, etc. — happy path returns the created child entity; duplicate
  input throws `InvariantViolationException`.
- **Mutation methods** — `Rename`, `UpdateText`, `RemoveMember`, `RemoveVote`
  — happy path mutates state; invalid input throws the appropriate exception.
- **Domain events (API 5 only)** — after a domain operation, the aggregate's
  `DomainEvents` collection contains the expected event type with the correct
  payload.

## What Is NOT Tested

- Persistence (EF Core, migrations) — covered by integration tests.
- HTTP endpoints, controllers, middleware — covered by integration tests.
- Application services / MediatR handlers — orchestration logic that depends
  on repositories and UoW; covered by integration tests.
- Cross-aggregate invariants enforced via DB constraints — integration tests.

## Test Inventory by Tier

### API 2 — Rich Entity Tests

Each entity is tested independently because in API 2 entities own their own
invariants but are not yet organized into aggregates.

| Test Class | Entity Methods Tested | Test Count |
|------------|----------------------|------------|
| `UserTests` | Constructor guards | 3 |
| `ProjectTests` | Constructor, `AddMember`, `RemoveMember` | 5 |
| `RetroBoardTests` | Constructor, `AddColumn` | 5 |
| `ColumnTests` | Constructor, `Rename`, `AddNote` | 7 |
| `NoteTests` | Constructor, `CastVote`, `RemoveVote`, `UpdateText` | 8 |
| `VoteTests` | Constructor | 1 |
| **Total** | | **~29** |

### API 3 — Aggregate Root Tests

All column/note/vote operations go through the `RetroBoard` aggregate root.
There are no separate `ColumnTests`, `NoteTests`, or `VoteTests`.

| Test Class | Aggregate Methods Tested | Test Count |
|------------|-------------------------|------------|
| `UserTests` | Constructor guards | 3 |
| `ProjectTests` | Constructor, `AddMember`, `RemoveMember` | 5 |
| `RetroBoardTests` | Constructor, `AddColumn`, `RenameColumn`, `RemoveColumn`, `AddNote`, `UpdateNote`, `RemoveNote`, `CastVote`, `RemoveVote` | ~17 |
| **Total** | | **~25** |

### API 4 — Split Aggregate Tests

Same as API 3, minus vote operations on `RetroBoard`. Vote is its own
aggregate and tested separately.

| Test Class | Aggregate Methods Tested | Test Count |
|------------|-------------------------|------------|
| `UserTests` | Constructor guards | 3 |
| `ProjectTests` | Constructor, `AddMember`, `RemoveMember` | 5 |
| `RetroBoardTests` | Constructor, `AddColumn`, `RenameColumn`, `RemoveColumn`, `AddNote`, `UpdateNote`, `RemoveNote` | ~14 |
| `VoteTests` | Constructor | 1 |
| **Total** | | **~23** |

### API 5 — Behavioral + Domain Event Tests

Same aggregate behavior as API 4, plus assertions that domain events are
raised correctly.

| Test Class | Additional Event Assertions | Test Count |
|------------|---------------------------|------------|
| `UserTests` | (none — User raises no events) | 3 |
| `ProjectTests` | `MemberAddedToProjectEvent`, `MemberRemovedFromProjectEvent` | 7 |
| `RetroBoardTests` | `ColumnAddedEvent`, `NoteAddedEvent`, `NoteRemovedEvent` | ~17 |
| `VoteTests` | `VoteCastEvent` | 2 |
| **Total** | | **~29** |

## Project Structure

```
tests/
├── Api2.Domain.UnitTests/
│   ├── UserTests.cs
│   ├── ProjectTests.cs
│   ├── RetroBoardTests.cs
│   ├── ColumnTests.cs
│   ├── NoteTests.cs
│   └── VoteTests.cs
├── Api3.Domain.UnitTests/
│   ├── UserTests.cs
│   ├── ProjectTests.cs
│   └── RetroBoardTests.cs
├── Api4.Domain.UnitTests/
│   ├── UserTests.cs
│   ├── ProjectTests.cs
│   ├── RetroBoardTests.cs
│   └── VoteTests.cs
└── Api5.Domain.UnitTests/
    ├── UserTests.cs
    ├── ProjectTests.cs
    ├── RetroBoardTests.cs
    └── VoteTests.cs
```

## Why No Shared Unit Test Base?

Unlike integration tests (where all 5 APIs share the same HTTP contract and
the same test cases), unit tests exercise **API-specific domain APIs** that
differ significantly between tiers:

- API 2 entities: `Column.AddNote()`, `Note.CastVote()`
- API 3 aggregate root: `RetroBoard.AddNote(columnId, text)`, `RetroBoard.CastVote(columnId, noteId, userId)`
- API 4 aggregate root: no vote methods on RetroBoard (Vote is its own aggregate)
- API 5 aggregate root: same as API 4, plus domain event assertions

A shared base would create artificial coupling between tiers that are meant to
be compared, not unified.

## Conventions

- **Namespace:** `Api{N}.Domain.UnitTests` — flat, no sub-namespaces.
- **Test naming:** `{Method}_{Condition}_{ExpectedResult}` — e.g.,
  `AddColumn_WithDuplicateName_ThrowsInvariantViolation`.
- **Assertions:** FluentAssertions (`.Should().Be()`, `.Should().Throw<>()`).
- **No mocking frameworks** — see [Why No Mocking?](why-no-mocking.md).

## Where to Go Next

- [Test Infrastructure](test-infrastructure.md) — How integration tests work
  (Testcontainers, Respawn, WebApplicationFactory).
- [Why No Mocking?](why-no-mocking.md) — Full analysis of the no-mocking
  decision.
- [Shared Test Pattern](shared-tests.md) — How integration tests share test
  cases across all five APIs.
