# Why This Repository Does Not Use Mocking

## The Question

> "Shouldn't we mock repositories and services so we can unit test
> command handlers and application services in isolation?"

This is one of the most common questions in .NET architecture discussions.
The answer for this repository is **no** — and the reasoning is deliberate,
not accidental.

---

## The Testing Strategy

This repository uses two test layers:

```
┌─────────────────────────────────────────┐
│       Integration Tests (API 1–5)       │  Verify wiring, DB, HTTP
│   Testcontainers · WebApplicationFactory │
├─────────────────────────────────────────┤
│       Domain Unit Tests (API 2–5)       │  Verify invariants, guards,
│   No infra · No mocks · Pure functions  │  domain events, pure logic
└─────────────────────────────────────────┘
```

| Layer | What It Tests | Speed | Infra Needed |
|-------|--------------|-------|-------------|
| Domain unit tests | Entity constructors, guard clauses, invariant methods, domain events | Milliseconds | None |
| Integration tests | Full HTTP → Controller → Service/Handler → DB round-trip | Seconds | Docker (Testcontainers) |

There is **no middle layer** of mock-based handler/service tests. This is
a conscious design choice.

---

## Why Not Mock?

### 1. Most Handlers Are Thin Orchestrators

The typical command handler in this codebase looks like this:

```csharp
// API 5 — AddColumnCommandHandler
public async Task<ColumnResponse> Handle(
    AddColumnCommand request, CancellationToken cancellationToken)
{
    RetroBoard retro = await _repository.GetByIdAsync(request.RetroBoardId, cancellationToken)
        ?? throw new NotFoundException("RetroBoard", request.RetroBoardId);

    Column column = retro.AddColumn(request.Name);   // Domain logic

    await _unitOfWork.SaveChangesAsync(cancellationToken);

    return new ColumnResponse(column.Id, column.Name, null);
}
```

A mock-based test would set up a fake `IRetroBoardRepository` that returns
a `RetroBoard`, verify that `GetByIdAsync` was called with the right ID,
verify that `SaveChangesAsync` was called, and assert the return value.

**This tests implementation details, not behavior.** If the handler is
refactored to use a different repository method (say `GetByIdWithColumnsAsync`
instead of `GetByIdAsync`), the mock test breaks — even though the behavior
is identical. This is the **brittle mock** anti-pattern described by
practitioners like Mark Seemann (*Dependency Injection: Principles, Practices,
and Patterns*) and Ian Cooper (*TDD: Where Did It All Go Wrong?*).

### 2. The Real Logic Is Already Unit Tested

The interesting part of the handler above is `retro.AddColumn(request.Name)`.
That method:

- Validates the name (guard clause)
- Checks for duplicate column names (invariant)
- Creates a new `Column` and adds it to the collection
- Raises a `ColumnAddedEvent` (API 5)

All of this is exercised by the **domain unit tests** — no mocking needed.
The handler merely loads the aggregate, calls the domain method, and saves.
A mock-based handler test would only verify the "load and save" wiring, not
the logic.

### 3. Orchestration Bugs Are Wiring Bugs

The bugs that mock-based handler tests could catch are wiring mistakes:

- "I forgot to call `SaveChangesAsync`"
- "I forgot to check for `null` after `GetByIdAsync`"
- "I mapped the wrong property to the response DTO"

These are **exactly the bugs that integration tests catch** — because the
integration test sends a real HTTP request and checks the real response
against a real database. Mock-based tests would miss the deeper wiring
issues (wrong DI registration, missing EF Core `Include`, broken query
filter interaction) that cause the same symptoms.

### 4. Query Handlers Must Not Be Mocked

API 5's query handlers project directly from `DbContext` using LINQ-to-SQL:

```csharp
// API 5 — GetRetroBoardQueryHandler
RetroBoardResponse? result = await _context.RetroBoards
    .AsNoTracking()
    .Where(r => r.Id == request.RetroBoardId)
    .Select(r => new RetroBoardResponse(
        r.Id, r.Name, r.ProjectId, r.CreatedAt,
        r.Columns.Select(c => new ColumnResponse(
            c.Id, c.Name,
            c.Notes.Select(n => new NoteResponse(
                n.Id, n.Text,
                _context.Votes.Count(v => v.NoteId == n.Id)
            )).ToList()
        )).ToList()
    ))
    .FirstOrDefaultAsync(cancellationToken);
```

Mocking `DbContext` or `DbSet<T>` is a well-documented anti-pattern. The
mocked LINQ provider behaves differently from EF Core's real query
translator — your test passes but the real query fails (or returns wrong
data). The EF Core team [recommends testing queries against a real
database](https://learn.microsoft.com/en-us/ef/core/testing/).

### 5. This Is Not Unique to API 5

A common argument is: "API 5 has MediatR handlers, so they're more
'testable' with mocks than services." But compare the actual code:

```csharp
// API 4 — VoteService.CastVoteAsync (service)
bool noteExists = await _retroRepository.NoteExistsAsync(noteId, ct);
if (!noteExists) throw new NotFoundException("Note", noteId);
// ... membership check, duplicate check, create vote, save

// API 5 — CastVoteCommandHandler.Handle (handler)
bool noteExists = await _retroRepository.NoteExistsAsync(request.NoteId, ct);
if (!noteExists) throw new NotFoundException("Note", request.NoteId);
// ... membership check, duplicate check, create vote, save
```

These are **nearly line-for-line identical**. The only difference is the
dispatch mechanism (direct call vs. MediatR). MediatR does not make handler
code more "mock-worthy" than service code. If mocking were justified for
handlers, it would be equally justified for services in API 2–4 — there is
no principled reason to mock only in API 5.

### 6. Educational Risk

In a teaching repository, introducing a mocking framework sends the implicit
message: "this is how you should test application services." Many developers
already over-mock — testing that `repository.GetByIdAsync()` was called
instead of testing observable behavior. This repository teaches a different
principle:

> **Design your domain so the interesting logic lives in pure functions
> that need no mocks. Verify the wiring with end-to-end tests.**

---

## When IS Mocking Justified?

Mocking is a valuable technique — just not for this codebase. It becomes
worthwhile when:

| Scenario | Why Mocks Help |
|----------|---------------|
| **Significant branching logic** in handlers | Multiple code paths with different outcomes (saga-style workflows, retry policies). Mock-based tests cheaply verify each branch. |
| **External service dependencies** | HTTP clients, message queues, file systems that are slow, non-deterministic, or unavailable in tests. Mocks make the test hermetic. |
| **Complex multi-aggregate coordination** | When the order of operations matters and must be verified (e.g., "charge payment BEFORE sending confirmation"). |
| **Slow or expensive integration tests** | When the integration test suite takes 30+ minutes, a fast mock-based test layer reduces the feedback loop for developers. |

This repository's handlers do not exhibit these characteristics. Most follow
a linear load → delegate → save pattern. The two handlers with the most
orchestration logic (`CastVoteCommandHandler` with 3 cross-aggregate checks,
and `NoteRemovedEventHandler` with conditional vote cleanup) are thoroughly
covered by the integration test suite.

---

## The Principle

The testing strategy follows a simple rule:

| Question | Answer | Test Layer |
|----------|--------|-----------|
| Does this method enforce a domain rule using only in-memory state? | Yes → | **Domain unit test** |
| Does this method orchestrate infrastructure (DB, HTTP, DI wiring)? | Yes → | **Integration test** |
| Does this method have complex branching that's expensive to set up end-to-end? | No → | No mock-based test needed |

If the answer to the third question were "yes" for any handler in this repo,
mock-based tests would be added for that specific handler. The decision is
**per-handler, not per-framework.**

---

## Further Reading

- Mark Seemann — [*Dependency Injection: Principles, Practices, and Patterns*](https://www.manning.com/books/dependency-injection-principles-practices-patterns) — Chapter on test doubles and when to use them.
- Ian Cooper — [*TDD: Where Did It All Go Wrong?*](https://www.youtube.com/watch?v=EZ05e7EMOLM) — The talk that distinguishes testing behavior vs. testing implementation.
- EF Core documentation — [*Testing against your production database system*](https://learn.microsoft.com/en-us/ef/core/testing/) — Why mocking `DbContext` is discouraged.
- Vladimir Khorikov — [*Unit Testing: Principles, Practices, and Patterns*](https://www.manning.com/books/unit-testing) — The "output-based vs. state-based vs. communication-based" testing taxonomy.

## Where to Go Next

- [Unit Tests](unit-tests.md) — How domain unit tests work in this repository.
- [Test Infrastructure](test-infrastructure.md) — Integration test setup with Testcontainers and Respawn.
- [Shared Test Pattern](shared-tests.md) — How integration tests are shared across all five APIs.
