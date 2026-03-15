# Specification Pattern

> **Used in:** API 5 only.
> Encapsulates business rules as composable, testable objects.

## The Problem

In API 1–4, voting eligibility rules are inline checks scattered across service methods (API 1–4) or command handlers:

```csharp
// API 4 — VoteService.CastVoteAsync (inline checks)
if (await _voteRepository.ExistsAsync(noteId, userId, ct))
    throw new InvariantViolationException("Already voted.");

if (project.Members.Count > 0 && !project.IsMember(userId))
    throw new InvariantViolationException("Not a project member.");
```

This works, but the rules are:
- **Not reusable** — each service/handler duplicates the same checks.
- **Not composable** — you can't combine rules with AND/OR/NOT.
- **Hard to test in isolation** — testing a single rule requires mocking the entire service dependency chain.
- **Rigid** — adding a new voting mode means modifying existing methods.

## The Solution

The **Specification pattern** encapsulates each business rule as a first-class object with a single method: `IsSatisfiedBy(T candidate)`.

```csharp
public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T candidate);
}
```

Each rule becomes its own class:

| Specification | Rule |
|--------------|------|
| `NoteExistsSpecification` | The target note must exist |
| `UserIsProjectMemberSpecification` | The user must be a project member |
| `UniqueVotePerNoteSpecification` | The user must not have already voted on this note |
| `VoteBudgetNotExceededSpecification` | The user must have remaining votes in the column |

## Boolean Composition

Specifications compose using three combinators:

| Combinator | Class | Extension | Semantics |
|-----------|-------|-----------|-----------|
| AND | `AndSpecification<T>` | `.And()` | Both must be satisfied |
| OR | `OrSpecification<T>` | `.Or()` | At least one must be satisfied |
| NOT | `NotSpecification<T>` | `.Not()` | Inverts the result |

```csharp
// Default strategy: note exists AND user is member AND vote is unique
ISpecification<VoteEligibilityContext> rule =
    new NoteExistsSpecification()
        .And(new UserIsProjectMemberSpecification())
        .And(new UniqueVotePerNoteSpecification());

bool canVote = rule.IsSatisfiedBy(context);
```

## The VoteEligibilityContext

Specifications evaluate a pre-built context rather than querying repositories directly. This separates **data gathering** (async, infrastructure) from **rule evaluation** (sync, pure domain):

```csharp
public record VoteEligibilityContext(
    Guid NoteId,
    Guid UserId,
    Guid ColumnId,
    Guid RetroBoardId,
    Guid ProjectId,
    bool NoteExists,
    bool UserIsProjectMember,
    bool UserAlreadyVotedOnNote,
    int UserVoteCountInColumn);
```

The command handler builds this context from repository queries, then passes it to the strategy for synchronous evaluation.

## How Specifications Work with Strategies

The Specification pattern pairs with the [Strategy pattern](strategy-pattern.md). Each voting strategy composes a **different subset** of the same reusable specifications:

| Strategy | Specifications Used |
|----------|-------------------|
| **Default** | NoteExists ∧ UserIsProjectMember ∧ UniqueVotePerNote |
| **Budget** | NoteExists ∧ UserIsProjectMember ∧ VoteBudgetNotExceeded |

The `DefaultVotingStrategy` includes `UniqueVotePerNoteSpecification` but the `BudgetVotingStrategy` omits it (allowing multiple votes on the same note) and substitutes `VoteBudgetNotExceededSpecification` instead. This difference in composition — enabled by the Specification pattern — is the key insight.

## Testing

Each specification is a pure function — no infrastructure, no mocking:

```csharp
[Fact]
public void UniqueVotePerNote_WhenAlreadyVoted_ReturnsFalse()
{
    var spec = new UniqueVotePerNoteSpecification();
    var context = new VoteEligibilityContext(
        ..., UserAlreadyVotedOnNote: true, ...);

    spec.IsSatisfiedBy(context).Should().BeFalse();
}
```

Composites are tested the same way:

```csharp
[Fact]
public void And_BothSatisfied_ReturnsTrue()
{
    var composite = specA.And(specB);
    composite.IsSatisfiedBy(candidate).Should().BeTrue();
}
```

## Rules vs Validate — Two Surfaces for the Same Logic

Each `IVotingStrategy` exposes the same business rules through two surfaces:

| Surface | Return type | Purpose |
|---------|------------|---------|
| `Rules` (property) | `ISpecification<VoteEligibilityContext>` → `bool` | Quick boolean eligibility check — no exceptions, no error messages |
| `Validate` (method) | `void` (throws on failure) | Detailed validation — throws a targeted domain exception on the **first** failing rule |

### Why both?

The **production path** (`CastVoteCommandHandler`) calls `Validate` because the API needs to return a Problem Details response with a specific error message ("User already voted", "Budget exceeded", etc.). Evaluating all rules as a flat `bool` would lose that diagnostic information.

The **`Rules` composite** is not on the current production path, but it demonstrates a core benefit of the Specification pattern — composability — and supports scenarios such as:

1. **Bulk eligibility filtering** — A query handler could loop over every note on a board and call `strategy.Rules.IsSatisfiedBy(ctx)` to build a list of "can vote" / "cannot vote" indicators for the UI, without throwing an exception per ineligible note.

2. **UI pre-flight checks** — A lightweight endpoint could return a boolean so the front end can disable the vote button *before* the user clicks it.

3. **Compound specifications** — A new strategy could wrap an existing strategy's rules: `existingStrategy.Rules.And(newSpec)`, reusing the composite without subclassing or duplicating specification wiring.

```csharp
// Example: bulk eligibility check (hypothetical query handler)
List<NoteEligibility> results = notes.Select(note =>
{
    VoteEligibilityContext ctx = BuildContext(note, userId);
    return new NoteEligibility(note.Id, strategy.Rules.IsSatisfiedBy(ctx));
}).ToList();
```

The unit tests exercise `Rules` directly to prove the composite evaluates correctly, independently from the exception-throwing path. This separation lets you test the composition logic without catching exceptions.

## Trade-offs

| Benefit | Cost |
|---------|------|
| Rules are reusable across strategies | More classes than inline checks |
| Rules are independently testable | Requires a context object for data |
| Composable with AND/OR/NOT | Indirection — must trace through composites |
| New rules = new classes (Open/Closed) | Slight learning curve for the pattern |
| Domain stays pure and synchronous | Handler must pre-fetch all data for the context |
| Dual surface (Rules + Validate) covers both boolean and exception scenarios | `Rules` is not on the production path yet — it's forward-looking |

## When to Use

Use the Specification pattern when:
- Business rules are composed differently in different contexts (like voting strategies).
- You need to reuse the same rule across multiple features.
- You want rules to be independently unit-testable without mocking.
- Rules may need to be dynamically composed at runtime.

Don't use it for a single, non-reusable validation check — that's over-engineering.
