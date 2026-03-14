# Options Pattern

> **Used in:** API 5 only.
> Binds configuration values from `appsettings.json` to strongly-typed C# objects and validates them at application startup.

## The Problem

In API 1–4, behavioral configuration is **hardcoded**:

```csharp
// ❌ Without Options — magic numbers scattered in code
public class BudgetVotingStrategy
{
    public const int MaxVotesPerColumn = 3;  // hardcoded, can't change without recompile
}
```

```csharp
// ❌ Without Options — default strategy hardcoded in controller
var command = new CreateRetroBoardCommand(
    projectId,
    request.Name,
    request.VotingStrategy ?? VotingStrategyType.Default); // hardcoded fallback
```

If an operator wants to change the default voting strategy or adjust the vote budget, they need a code change and a redeployment. This violates the **externalized configuration** principle.

## The Solution

The **Options pattern** (`IOptions<T>`) gives you:

1. **Strongly-typed configuration** — no `config["Voting:MaxVotesPerColumn"]` string lookups
2. **Startup validation** — misconfiguration fails the app immediately, not at runtime
3. **Testability** — inject `IOptions<T>` and override values in test fixtures

### 1. Define the Options class

```csharp
public class VotingOptions
{
    public const string SectionName = "Voting";

    public VotingStrategyType DefaultVotingStrategy { get; set; }
        = VotingStrategyType.Default;

    public int MaxVotesPerColumn { get; set; } = 3;
}
```

### 2. Configure in `appsettings.json`

```json
{
  "Voting": {
    "DefaultVotingStrategy": "Default",
    "MaxVotesPerColumn": 3
  }
}
```

### 3. Validate with `IValidateOptions<T>`

```csharp
public class VotingOptionsValidator : IValidateOptions<VotingOptions>
{
    public ValidateOptionsResult Validate(string? name, VotingOptions options)
    {
        if (!Enum.IsDefined(typeof(VotingStrategyType), options.DefaultVotingStrategy))
            return ValidateOptionsResult.Fail(
                $"Voting:DefaultVotingStrategy '{options.DefaultVotingStrategy}' is not valid.");

        if (options.MaxVotesPerColumn <= 0)
            return ValidateOptionsResult.Fail(
                $"Voting:MaxVotesPerColumn must be > 0, was {options.MaxVotesPerColumn}.");

        return ValidateOptionsResult.Success;
    }
}
```

### 4. Register in `Program.cs`

```csharp
builder.Services
    .AddOptions<VotingOptions>()
    .Bind(builder.Configuration.GetSection(VotingOptions.SectionName))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<VotingOptions>, VotingOptionsValidator>();
```

The key line is **`.ValidateOnStart()`** — this runs all `IValidateOptions<T>` implementations during application startup. A misconfigured `appsettings.json` will crash the app immediately with a descriptive error, not silently produce wrong behavior when a user casts a vote.

## How It's Used

### In Command Handlers

The `CreateRetroBoardCommandHandler` uses the configured default strategy when the caller doesn't specify one:

```csharp
public class CreateRetroBoardCommandHandler : IRequestHandler<CreateRetroBoardCommand, RetroBoardResponse>
{
    private readonly VotingOptions _votingOptions;

    public CreateRetroBoardCommandHandler(..., IOptions<VotingOptions> votingOptions)
    {
        _votingOptions = votingOptions.Value;
    }

    public async Task<RetroBoardResponse> Handle(CreateRetroBoardCommand request, ...)
    {
        VotingStrategyType strategyType =
            request.VotingStrategyType ?? _votingOptions.DefaultVotingStrategy;

        var retroBoard = new RetroBoard(request.ProjectId, request.Name, strategyType);
        // ...
    }
}
```

The `CastVoteCommandHandler` passes `MaxVotesPerColumn` through to the strategy factory:

```csharp
IVotingStrategy strategy = VotingStrategyFactory.Create(
    retro.VotingStrategyType,
    _votingOptions.MaxVotesPerColumn);
```

### In the Database Schema

The Options pattern even influences the database schema. The `RetroBoardDbContext` receives `IOptions<VotingOptions>` and conditionally applies a unique index on `Vote(NoteId, UserId)`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(RetroBoardDbContext).Assembly);

    // Unique constraint only when Default strategy is configured
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
}
```

When the default is **Default**, the unique index acts as a DB safety net against concurrent duplicate votes. When the default is **Budget**, the index stays non-unique because dot voting allows multiple votes per user per note.

### EF Core Model Cache Key

Because the database schema depends on configuration, different `VotingOptions` values need different EF Core models. A custom `IModelCacheKeyFactory` ensures the model is rebuilt per configuration:

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

Without this, EF Core would cache the first model built and reuse it for all configurations — breaking the conditional index logic.

## In Tests

The Options pattern makes test configuration explicit. Each test fixture configures `VotingOptions` to match its needs:

```csharp
// Api5Fixture — Default strategy, unique constraint active
protected override void ConfigureServices(IServiceCollection services)
{
    services.Configure<VotingOptions>(opts =>
    {
        opts.DefaultVotingStrategy = VotingStrategyType.Default;
        opts.MaxVotesPerColumn = 3;
    });
    // ... DbContext registration ...
}

// Api5BudgetFixture — Budget strategy, no unique constraint
protected override void ConfigureServices(IServiceCollection services)
{
    services.Configure<VotingOptions>(opts =>
    {
        opts.DefaultVotingStrategy = VotingStrategyType.Budget;
        opts.MaxVotesPerColumn = 3;
    });
    // ... DbContext registration ...
}
```

The same `Configure<T>` mechanism used in production (`appsettings.json` binding) is used in tests (lambda overrides). This means tests exercise the **real options pipeline**, not a mock.

## Options Validation: Why `IValidateOptions<T>`?

.NET offers three approaches to options validation:

| Approach | Mechanism | Pros | Cons |
|----------|-----------|------|------|
| **Data Annotations** | `[Required]`, `[Range]` | Simple, declarative | Limited expressiveness, no cross-property rules |
| **`IValidateOptions<T>`** | Custom validation class | Full control, cross-property rules, testable | More code |
| **Delegate** | `.Validate(o => o.Max > 0)` | Inline, quick | No descriptive error messages, hard to test |

This repository uses `IValidateOptions<T>` because:
1. It produces **descriptive error messages** that name the specific config key
2. It supports **cross-property validation** (e.g., "Budget strategy requires MaxVotesPerColumn > 0")
3. The validator is a **unit-testable class** — not a lambda buried in `Program.cs`

## Architecture Impact

The Options pattern connects several layers:

```
┌─────────────────────────────────────────────────────────────┐
│  appsettings.json                                           │
│    "Voting": { "DefaultVotingStrategy": "Default",          │
│                "MaxVotesPerColumn": 3 }                     │
└────────────────────────┬────────────────────────────────────┘
                         │ Bind + Validate
┌────────────────────────▼────────────────────────────────────┐
│  Program.cs                                                  │
│    AddOptions<VotingOptions>().Bind(section).ValidateOnStart │
│    AddSingleton<IValidateOptions<VotingOptions>, Validator>  │
└────────────────────────┬────────────────────────────────────┘
                         │ IOptions<VotingOptions>
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
  ┌───────────┐  ┌──────────────┐  ┌──────────────────┐
  │ CreateRB  │  │ CastVote     │  │ RetroBoardDb     │
  │ Handler   │  │ Handler      │  │ Context          │
  │           │  │              │  │                  │
  │ default   │  │ maxVotes     │  │ conditional      │
  │ strategy  │  │ perColumn    │  │ unique index     │
  └───────────┘  └──────────────┘  └──────────────────┘
```

## Trade-offs

| Benefit | Cost |
|---------|------|
| Externalized configuration — change behavior without recompile | One more class (`VotingOptions`) + validator |
| Startup validation — fail fast on misconfiguration | `ValidateOnStart()` adds startup latency (negligible) |
| Testable — override options per fixture | Must remember to register options in test fixtures |
| Type-safe — no `config["key"]` string lookups | Options class must stay in sync with `appsettings.json` |
| Schema-aware — DB constraints adapt to config | Requires custom `IModelCacheKeyFactory` for EF Core model caching |

## Compare with API 1–4

| Aspect | API 1–4 | API 5 |
|--------|---------|-------|
| Default strategy | Hardcoded `VotingStrategyType.Default` | Configurable via `appsettings.json` |
| Vote budget | `const int` in strategy class | `VotingOptions.MaxVotesPerColumn` |
| Validation | None — invalid values discovered at runtime | `IValidateOptions<T>` + `ValidateOnStart()` |
| Test control | Hardcoded behavior | `services.Configure<VotingOptions>(...)` per fixture |
