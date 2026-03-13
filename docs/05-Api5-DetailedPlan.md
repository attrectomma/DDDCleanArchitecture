# API 5 — Behavior-Centric + MediatR (Detailed Implementation Plan)

> **Theme:** Move away from noun/entity-centric design toward **behavior-centric**
> design. Introduce the **Mediator pattern** (MediatR) with Commands, Queries,
> and Domain Events. The Application layer is organized around use cases,
> not entity services.

---

## 1. What Changes from API 4

| Aspect | API 4 | API 5 |
|--------|-------|-------|
| Application layer | Services per aggregate | **Command/Query handlers per use case** |
| Mediator | None | **MediatR** |
| Organization | Noun-centric (`VoteService`, `RetroBoardService`) | Behavior-centric (`CastVoteCommand`, `AddColumnCommand`) |
| Domain Events | None | **Yes** — aggregates raise events, handlers react |
| Side effects | Inline in services | Decoupled via domain event handlers |
| Controllers | Call services | **Send commands/queries via `IMediator`** |

> **DESIGN:** This tier demonstrates that as systems grow, organizing code
> around *what the system does* (behaviors/use cases) is more maintainable
> than organizing around *what the system has* (entities/nouns). MediatR
> provides the pipeline to achieve this with cross-cutting concerns
> (logging, validation, transactions) as pipeline behaviors.

---

## 2. Project Structure

```
src/Api5.Behavioral/
├── Api5.Domain/
│   ├── Common/
│   │   ├── AuditableEntityBase.cs
│   │   ├── IAggregateRoot.cs
│   │   ├── Entity.cs
│   │   ├── Guard.cs
│   │   ├── IDomainEvent.cs                        ← NEW
│   │   └── IHasDomainEvents.cs                    ← NEW
│   ├── UserAggregate/
│   │   ├── User.cs
│   │   └── IUserRepository.cs
│   ├── ProjectAggregate/
│   │   ├── Project.cs
│   │   ├── ProjectMember.cs
│   │   ├── IProjectRepository.cs
│   │   └── Events/
│   │       ├── MemberAddedToProjectEvent.cs       ← NEW
│   │       └── MemberRemovedFromProjectEvent.cs   ← NEW
│   ├── RetroAggregate/
│   │   ├── RetroBoard.cs
│   │   ├── Column.cs
│   │   ├── Note.cs
│   │   ├── IRetroBoardRepository.cs
│   │   └── Events/
│   │       ├── ColumnAddedEvent.cs                ← NEW
│   │       ├── NoteAddedEvent.cs                  ← NEW
│   │       └── NoteRemovedEvent.cs                ← NEW
│   ├── VoteAggregate/
│   │   ├── Vote.cs
│   │   ├── IVoteRepository.cs
│   │   └── Events/
│   │       └── VoteCastEvent.cs                   ← NEW
│   └── Api5.Domain.csproj
│
├── Api5.Application/
│   ├── Common/
│   │   ├── Behaviors/                             ← NEW (MediatR pipeline)
│   │   │   ├── LoggingBehavior.cs
│   │   │   ├── ValidationBehavior.cs
│   │   │   └── TransactionBehavior.cs
│   │   ├── Interfaces/
│   │   │   └── IUnitOfWork.cs
│   │   └── Exceptions/
│   │       └── NotFoundException.cs
│   │
│   ├── Users/                                     ← Organized by feature/use-case
│   │   ├── Commands/
│   │   │   └── CreateUser/
│   │   │       ├── CreateUserCommand.cs
│   │   │       ├── CreateUserCommandHandler.cs
│   │   │       └── CreateUserCommandValidator.cs
│   │   └── Queries/
│   │       └── GetUser/
│   │           ├── GetUserQuery.cs
│   │           └── GetUserQueryHandler.cs
│   │
│   ├── Projects/
│   │   ├── Commands/
│   │   │   ├── CreateProject/
│   │   │   │   ├── CreateProjectCommand.cs
│   │   │   │   ├── CreateProjectCommandHandler.cs
│   │   │   │   └── CreateProjectCommandValidator.cs
│   │   │   ├── AddMember/
│   │   │   │   ├── AddMemberCommand.cs
│   │   │   │   └── AddMemberCommandHandler.cs
│   │   │   └── RemoveMember/
│   │   │       ├── RemoveMemberCommand.cs
│   │   │       └── RemoveMemberCommandHandler.cs
│   │   └── Queries/
│   │       └── GetProject/
│   │           ├── GetProjectQuery.cs
│   │           └── GetProjectQueryHandler.cs
│   │
│   ├── Retros/
│   │   ├── Commands/
│   │   │   ├── CreateRetroBoard/
│   │   │   ├── AddColumn/
│   │   │   ├── RenameColumn/
│   │   │   ├── RemoveColumn/
│   │   │   ├── AddNote/
│   │   │   ├── UpdateNote/
│   │   │   └── RemoveNote/
│   │   └── Queries/
│   │       └── GetRetroBoard/
│   │
│   ├── Votes/
│   │   ├── Commands/
│   │   │   ├── CastVote/
│   │   │   │   ├── CastVoteCommand.cs
│   │   │   │   ├── CastVoteCommandHandler.cs
│   │   │   │   └── CastVoteCommandValidator.cs
│   │   │   └── RemoveVote/
│   │   │       ├── RemoveVoteCommand.cs
│   │   │       └── RemoveVoteCommandHandler.cs
│   │   └── EventHandlers/                         ← NEW
│   │       └── NoteRemovedEventHandler.cs         (cleanup votes when note deleted)
│   │
│   ├── DTOs/                                      (shared response DTOs)
│   └── Api5.Application.csproj
│
├── Api5.Infrastructure/
│   ├── Persistence/
│   │   ├── RetroBoardDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   ├── DomainEventDispatcher.cs               ← NEW
│   │   ├── Configurations/
│   │   ├── Interceptors/
│   │   │   ├── AuditInterceptor.cs
│   │   │   └── DomainEventInterceptor.cs          ← NEW
│   │   └── Repositories/
│   └── Api5.Infrastructure.csproj
│
└── Api5.WebApi/
    ├── Controllers/
    │   ├── UsersController.cs
    │   ├── ProjectsController.cs
    │   ├── RetroBoardsController.cs
    │   └── VotesController.cs
    ├── Middleware/
    ├── Program.cs
    └── Api5.WebApi.csproj
```

---

## 3. Domain Events

### 3.1 Base Types

```csharp
/// <summary>
/// Marker interface for domain events — things that "happened"
/// within an aggregate that other parts of the system may care about.
/// </summary>
/// <remarks>
/// DESIGN: Domain events decouple side effects from the aggregate
/// that raises them. In API 4, when a note is deleted, the VoteService
/// had to explicitly handle vote cleanup. With domain events, the
/// Note aggregate raises a NoteRemovedEvent and a handler reacts.
/// This follows the Open/Closed Principle — new reactions can be
/// added without modifying the aggregate.
/// </remarks>
public interface IDomainEvent : INotification  // MediatR INotification
{
    DateTime OccurredOn { get; }
}

/// <summary>Interface for entities that can raise domain events.</summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

### 3.2 Aggregate Base with Events

```csharp
/// <summary>
/// Enhanced aggregate root base that supports domain event collection.
/// </summary>
public abstract class AggregateRoot : AuditableEntityBase, IAggregateRoot, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Registers a domain event to be dispatched after save.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### 3.3 Example Events

```csharp
/// <summary>Raised when a vote is cast on a note.</summary>
public record VoteCastEvent(Guid VoteId, Guid NoteId, Guid UserId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

/// <summary>Raised when a note is removed from a column.</summary>
public record NoteRemovedEvent(Guid NoteId, Guid ColumnId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
```

---

## 4. MediatR Commands & Handlers

### 4.1 Command (example: CastVote)

```csharp
/// <summary>
/// Command to cast a vote on a note.
/// </summary>
/// <remarks>
/// DESIGN: Commands are simple immutable data objects. They describe
/// the user's INTENT, not the implementation. This is the key mindset
/// shift from noun-centric (VoteService.CastVote) to behavior-centric
/// (CastVoteCommand). The command name reads as a sentence:
/// "Cast a vote on note X by user Y."
/// </remarks>
public record CastVoteCommand(Guid NoteId, Guid UserId) : IRequest<VoteResponse>;
```

### 4.2 Handler

```csharp
/// <summary>
/// Handles the <see cref="CastVoteCommand"/> by creating a Vote aggregate
/// and persisting it.
/// </summary>
/// <remarks>
/// DESIGN: Each handler is a focused unit of work for a single use case.
/// Compare with API 4's VoteService which handled both CastVote and
/// RemoveVote. Here, each operation has its own handler, making it
/// easier to:
///   - Test in isolation
///   - Add cross-cutting concerns via pipeline behaviors
///   - Reason about a single code path
///
/// The handler's logic is identical to API 4's VoteService.CastVoteAsync,
/// but the structure is fundamentally different.
/// </remarks>
public class CastVoteCommandHandler : IRequestHandler<CastVoteCommand, VoteResponse>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IRetroBoardRepository _retroRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<VoteResponse> Handle(
        CastVoteCommand command, CancellationToken ct)
    {
        // Cross-aggregate validation (same as API 4)
        var noteExists = await _retroRepository.NoteExistsAsync(command.NoteId, ct);
        if (!noteExists)
            throw new NotFoundException("Note", command.NoteId);

        if (await _voteRepository.ExistsAsync(command.NoteId, command.UserId, ct))
            throw new InvariantViolationException(
                $"User {command.UserId} has already voted on note {command.NoteId}.");

        var vote = new Vote(command.NoteId, command.UserId);
        await _voteRepository.AddAsync(vote, ct);

        // Domain event is raised in the Vote constructor or here
        // and will be dispatched by the DomainEventInterceptor after save.

        await _unitOfWork.SaveChangesAsync(ct);
        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }
}
```

### 4.3 Validator (FluentValidation + MediatR pipeline)

```csharp
/// <summary>
/// Validates <see cref="CastVoteCommand"/> before the handler executes.
/// </summary>
/// <remarks>
/// DESIGN: Validation is a pipeline behavior, not inline code.
/// The handler never receives an invalid command.
/// </remarks>
public class CastVoteCommandValidator : AbstractValidator<CastVoteCommand>
{
    public CastVoteCommandValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
```

---

## 5. MediatR Pipeline Behaviors

### 5.1 Validation Behavior

```csharp
/// <summary>
/// MediatR pipeline behavior that runs FluentValidation validators
/// before the handler executes.
/// </summary>
/// <remarks>
/// DESIGN: This replaces the manual validation calls in service methods
/// (API 1-4). Every command/query automatically gets validated if a
/// matching IValidator is registered. This is the Open/Closed Principle
/// in action — new validators are discovered by convention.
/// </remarks>
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

### 5.2 Logging Behavior

```csharp
/// <summary>
/// Logs every command/query entering and leaving the pipeline.
/// </summary>
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        _logger.LogInformation("Handling {RequestName}: {@Request}",
            typeof(TRequest).Name, request);

        var response = await next();

        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

### 5.3 Transaction Behavior

```csharp
/// <summary>
/// Wraps command handlers in an explicit database transaction.
/// </summary>
/// <remarks>
/// DESIGN: In API 1-4, each service method implicitly used EF Core's
/// transaction-per-SaveChanges. Here, we make transactions explicit
/// and ensure domain events are dispatched within the same transaction.
/// </remarks>
public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Only wrap commands (not queries) in transactions
        if (!typeof(TRequest).Name.EndsWith("Command"))
            return await next();

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var response = await next();
        await transaction.CommitAsync(ct);
        return response;
    }
}
```

---

## 6. Domain Event Dispatching

```csharp
/// <summary>
/// EF Core interceptor that dispatches domain events after SaveChanges.
/// </summary>
/// <remarks>
/// DESIGN: Domain events are dispatched AFTER the aggregate is persisted
/// but WITHIN the same transaction (thanks to TransactionBehavior).
/// This ensures handlers see committed state. If a handler fails,
/// the entire transaction rolls back.
///
/// Events are dispatched via MediatR's IPublisher, which finds all
/// INotificationHandler<T> implementations.
/// </remarks>
public class DomainEventInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken ct)
    {
        var context = eventData.Context!;
        var entities = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        entities.ForEach(e => e.ClearDomainEvents());

        foreach (var domainEvent in events)
        {
            await _publisher.Publish(domainEvent, ct);
        }

        return await base.SavedChangesAsync(eventData, result, ct);
    }
}
```

---

## 7. Domain Event Handlers (example)

```csharp
/// <summary>
/// When a note is removed, clean up all associated votes.
/// </summary>
/// <remarks>
/// DESIGN: In API 4, this cleanup was either:
///   - Manual (VoteService had to know about note deletion), or
///   - Handled by DB cascade delete.
/// With domain events, the cleanup is decoupled. The RetroBoard aggregate
/// raises NoteRemovedEvent, and this handler reacts without the retro
/// aggregate knowing anything about votes.
/// </remarks>
public class NoteRemovedEventHandler : INotificationHandler<NoteRemovedEvent>
{
    private readonly IVoteRepository _voteRepository;

    public async Task Handle(NoteRemovedEvent notification, CancellationToken ct)
    {
        var votes = await _voteRepository.GetByNoteIdAsync(notification.NoteId, ct);
        foreach (var vote in votes)
        {
            _voteRepository.Delete(vote);
        }
    }
}
```

---

## 8. Controllers (thin, dispatch via MediatR)

```csharp
/// <summary>
/// Controller that dispatches commands and queries via MediatR.
/// No service dependencies — only IMediator.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 1's controllers that depend on IColumnService,
/// INoteService, etc. Here, the controller depends only on IMediator and
/// doesn't know what handler will process the request. This decoupling
/// makes it trivial to add new operations without modifying the controller.
/// </remarks>
[ApiController]
[Route("api/notes/{noteId:guid}/votes")]
public class VotesController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> CastVote(
        Guid noteId, CastVoteRequest request, CancellationToken ct)
    {
        var command = new CastVoteCommand(noteId, request.UserId);
        var response = await _mediator.Send(command, ct);
        return CreatedAtAction(/* ... */);
    }

    [HttpDelete("{voteId:guid}")]
    public async Task<IActionResult> RemoveVote(
        Guid noteId, Guid voteId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveVoteCommand(noteId, voteId), ct);
        return NoContent();
    }
}
```

---

## 9. DI Registration (Program.cs)

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CastVoteCommand).Assembly);
});

// Pipeline behaviors (order matters)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

// FluentValidation — auto-discover validators
builder.Services.AddValidatorsFromAssembly(typeof(CastVoteCommandValidator).Assembly);

// Repositories, UoW, DbContext — same as API 4
```

---

## 10. Complete Command / Query Inventory

### Commands (writes)

| Feature Area | Command | Handler |
|-------------|---------|---------|
| Users | `CreateUserCommand` | `CreateUserCommandHandler` |
| Projects | `CreateProjectCommand` | `CreateProjectCommandHandler` |
| Projects | `AddMemberCommand` | `AddMemberCommandHandler` |
| Projects | `RemoveMemberCommand` | `RemoveMemberCommandHandler` |
| Retros | `CreateRetroBoardCommand` | `CreateRetroBoardCommandHandler` |
| Retros | `AddColumnCommand` | `AddColumnCommandHandler` |
| Retros | `RenameColumnCommand` | `RenameColumnCommandHandler` |
| Retros | `RemoveColumnCommand` | `RemoveColumnCommandHandler` |
| Retros | `AddNoteCommand` | `AddNoteCommandHandler` |
| Retros | `UpdateNoteCommand` | `UpdateNoteCommandHandler` |
| Retros | `RemoveNoteCommand` | `RemoveNoteCommandHandler` |
| Votes | `CastVoteCommand` | `CastVoteCommandHandler` |
| Votes | `RemoveVoteCommand` | `RemoveVoteCommandHandler` |

### Queries (reads)

| Feature Area | Query | Handler |
|-------------|-------|---------|
| Users | `GetUserQuery` | `GetUserQueryHandler` |
| Projects | `GetProjectQuery` | `GetProjectQueryHandler` |
| Retros | `GetRetroBoardQuery` | `GetRetroBoardQueryHandler` |

### Domain Event Handlers

| Event | Handler | Purpose |
|-------|---------|---------|
| `NoteRemovedEvent` | `NoteRemovedEventHandler` | Clean up orphaned votes |
| `VoteCastEvent` | (optional) Logging / notifications | |
| `MemberRemovedFromProjectEvent` | (optional) Revoke retro access | |

---

## 11. What This Tier Solves vs API 4

| Improvement | How |
|-------------|-----|
| Scattered cross-cutting concerns | Pipeline behaviors (logging, validation, transactions) |
| Tight coupling between controllers and services | Controllers only know IMediator |
| Side effects coupled to primary operations | Domain events decouple reactions |
| Feature organization | Use-case folders instead of service classes |
| Testability | Each handler is independently unit-testable |

---

## 12. Trade-offs

| Trade-off | Description |
|-----------|------------|
| **More files** | Each use case = command + handler + validator = 3 files (13 commands × 3 ≈ 39 files in Application layer) |
| **Indirection** | Request → MediatR → pipeline → handler is harder to trace than service.Method() |
| **Learning curve** | MediatR, pipeline behaviors, and domain events are concepts juniors need to learn |
| **Debugging** | Stack traces go through MediatR internals |

> **Key teaching point:** Behavior-centric design shines in **larger systems**
> where features are added frequently and cross-cutting concerns are many.
> For this small domain it may feel like over-engineering — and that's
> a valid observation worth discussing.

---

## 13. Files to Create

| Layer | File Count | Key New Files |
|-------|-----------|---------------|
| Domain | ~18 | `IDomainEvent.cs`, `IHasDomainEvents.cs`, `AggregateRoot.cs`, all event records |
| Application | ~45 | 13 command/handler/validator sets, 3 query/handler sets, 3 pipeline behaviors, event handlers |
| Infrastructure | ~12 | Same as API 4 + `DomainEventInterceptor.cs`, `DomainEventDispatcher.cs` |
| WebApi | ~6 | Same controllers but using IMediator |
