# Strategy Pattern

> **Used in:** API 5 only.
> Encapsulates a family of algorithms (voting rules) behind a common interface, allowing the algorithm to vary independently from the clients that use it.

## The Problem

In API 1–4, voting rules are hardcoded: **one vote per user per note**. If you wanted to support a different voting mode (e.g., dot voting), you'd need to modify the existing service or handler with conditional logic:

```csharp
// ❌ Without Strategy — conditional branching
if (board.VotingMode == "Budget")
{
    // check budget...
}
else
{
    // check uniqueness...
}
```

Every new voting mode adds another branch. The method grows, becomes harder to test, and violates the Open/Closed Principle.

## The Solution

The **Strategy pattern** extracts each voting mode into its own class behind a common interface:

```csharp
public interface IVotingStrategy
{
    ISpecification<VoteEligibilityContext> Rules { get; }
    void Validate(VoteEligibilityContext context);
}
```

Each strategy defines which rules apply:

| Strategy | Class | Behaviour |
|----------|-------|-----------|
| **Default** | `DefaultVotingStrategy` | One vote per user per note (API 1–4 behaviour) |
| **Budget** | `BudgetVotingStrategy` | Max 3 votes per user per column; multiple votes on the same note allowed ("dot voting") |

## How It Works

### 1. RetroBoard stores the strategy type

```csharp
public class RetroBoard : AggregateRoot
{
    public VotingStrategyType VotingStrategyType { get; private set; }
        = VotingStrategyType.Default;

    public void SetVotingStrategy(VotingStrategyType strategyType)
    {
        VotingStrategyType = strategyType;
    }
}
```

### 2. Factory resolves the strategy

```csharp
public static class VotingStrategyFactory
{
    public static IVotingStrategy Create(VotingStrategyType type) => type switch
    {
        VotingStrategyType.Default => new DefaultVotingStrategy(),
        VotingStrategyType.Budget  => new BudgetVotingStrategy(),
        _ => throw new DomainException($"Unknown voting strategy: {type}")
    };
}
```

### 3. Handler delegates to the strategy

```csharp
public class CastVoteCommandHandler : IRequestHandler<CastVoteCommand, VoteResponse>
{
    public async Task<VoteResponse> Handle(CastVoteCommand request, CancellationToken ct)
    {
        // 1. Load retro board (determines strategy)
        RetroBoard retro = await _retroRepo.GetByNoteIdAsync(request.NoteId, ct);

        // 2. Build eligibility context from repository data
        var context = new VoteEligibilityContext(...);

        // 3. Resolve strategy and validate
        IVotingStrategy strategy = VotingStrategyFactory.Create(retro.VotingStrategyType);
        strategy.Validate(context);  // throws on failure

        // 4. Create and persist vote
        var vote = new Vote(request.NoteId, request.UserId);
        await _voteRepo.AddAsync(vote, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }
}
```

### 4. Strategies compose specifications differently

```csharp
// DefaultVotingStrategy — enforces uniqueness
public ISpecification<VoteEligibilityContext> Rules =>
    noteExists.And(isMember).And(uniqueVote);

// BudgetVotingStrategy — enforces budget, allows duplicates
public ISpecification<VoteEligibilityContext> Rules =>
    noteExists.And(isMember).And(budgetNotExceeded);
```

## Strategy + Specification: Working Together

The Strategy pattern selects **which rules** apply. The [Specification pattern](specification-pattern.md) makes each rule **composable and testable**:

```
┌─────────────────────────────────────────────────────┐
│  CastVoteCommandHandler                             │
│                                                     │
│  1. Build VoteEligibilityContext (async, repos)     │
│  2. Resolve IVotingStrategy from board config       │
│  3. strategy.Validate(context)                      │
│     ┌───────────────────────────────────────────┐   │
│     │  DefaultVotingStrategy                    │   │
│     │  Rules: NoteExists ∧ IsMember ∧ Unique   │   │
│     └───────────────────────────────────────────┘   │
│     ┌───────────────────────────────────────────┐   │
│     │  BudgetVotingStrategy                     │   │
│     │  Rules: NoteExists ∧ IsMember ∧ Budget   │   │
│     └───────────────────────────────────────────┘   │
│  4. Create Vote + persist                           │
└─────────────────────────────────────────────────────┘
```

## Database Constraint Trade-off

In API 4, a **unique index** on `(NoteId, UserId)` served as a safety net for the one-vote-per-note rule. With configurable strategies, this constraint was removed because the `BudgetVotingStrategy` allows multiple votes on the same note.

| Aspect | API 4 | API 5 |
|--------|-------|-------|
| Uniqueness enforcement | DB unique index + app check | App-level specification only |
| Race condition protection | DB catches concurrent duplicates | Application-level check may race |
| Multiple votes per note | ❌ Impossible | ✅ With Budget strategy |

The `DefaultVotingStrategy` enforces uniqueness via `UniqueVotePerNoteSpecification` at the application level. Under extreme concurrency, a rare race condition could allow a duplicate. In production, mitigate with serializable transactions or advisory locks. This is a real-world trade-off of configurable business rules.

## Adding a New Strategy

Adding a third voting strategy (e.g., "Ranked Choice") requires:

1. Add `RankedChoice` to the `VotingStrategyType` enum.
2. Create `RankedChoiceVotingStrategy : IVotingStrategy` with its specification composition.
3. Add the new case to `VotingStrategyFactory`.
4. (Optional) Create new specifications if the strategy needs rules that don't exist yet.

**No existing code is modified** — the handler, controller, and existing strategies are untouched. This is the Open/Closed Principle in action.

## Testing

Strategies are pure domain objects — testable without infrastructure:

```csharp
[Fact]
public void Validate_AlreadyVotedOnNote_BudgetStrategy_DoesNotThrow()
{
    var strategy = new BudgetVotingStrategy();
    var context = new VoteEligibilityContext(
        ..., UserAlreadyVotedOnNote: true, UserVoteCountInColumn: 1, ...);

    Action act = () => strategy.Validate(context);

    act.Should().NotThrow(); // Budget allows duplicates
}
```

## Trade-offs

| Benefit | Cost |
|---------|------|
| Open/Closed — new strategies without changing existing code | More classes (strategy + factory) |
| Each strategy is independently testable | Indirection through factory + interface |
| Board-level configuration | DB unique constraint removed (race condition risk) |
| Clear separation of rules per voting mode | Must understand Strategy + Specification together |
