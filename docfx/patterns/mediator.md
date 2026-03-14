# Mediator Pattern

## Intent

Decouple the **sender** of a request from the **handler** that processes it.
Instead of calling a service directly, the sender publishes a message (command
or query) and a mediator dispatches it to the correct handler.

## In This Repository

API 5 uses **MediatR** — the most popular mediator library for .NET.

### Without Mediator (API 1–4)

```csharp
// Controller knows about the specific service
public class ColumnsController : ControllerBase
{
    private readonly IColumnService _service;

    [HttpPost]
    public async Task<IActionResult> Create(CreateColumnRequest request, CancellationToken ct)
    {
        var result = await _service.CreateAsync(retroId, request, ct);
        return CreatedAtAction(...);
    }
}
```

### With Mediator (API 5)

```csharp
// Controller only knows about IMediator
public class ColumnsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateColumnRequest request, CancellationToken ct)
    {
        var command = new AddColumnCommand(retroId, request.Name);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(...);
    }
}
```

## Pipeline Behaviors

The real power of MediatR is the **pipeline** — cross-cutting behaviors that
run around every handler:

```
Request → LoggingBehavior → ValidationBehavior → TransactionBehavior → Handler
```

Each behavior is a decorator:

```csharp
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Run BEFORE the handler
        var failures = _validators.Select(v => v.Validate(request))...;
        if (failures.Any()) throw new ValidationException(failures);

        // Call the actual handler
        return await next();
    }
}
```

### Selective Activation via `ICommand<T>`

Not all behaviors should run for all requests. The `TransactionBehavior` uses
a generic constraint to activate **only for commands**:

```csharp
// Only runs for types implementing ICommand<T>
public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>  // ← queries skip this entirely
```

Commands implement `ICommand<T>` (which extends `IRequest<T>`); queries
implement `IRequest<T>` directly. This makes the CQRS read/write split
visible at the type system level — not just by naming convention.

For the full transaction story, see
[Transaction Behavior](transaction-behavior.md).

## Benefits

- **Single Responsibility** — Each handler does one thing.
- **Open/Closed Principle** — Add new operations without modifying existing code.
- **Cross-cutting concerns** — Logging, validation, transactions, caching — all via pipeline behaviors.
- **Testability** — Each handler is independently unit-testable.

## Trade-offs

- **Indirection** — Harder to navigate (Ctrl+Click doesn't go from command to handler).
- **More files** — Each operation = command + handler + validator.
- **Magic** — DI auto-discovery of handlers can be opaque.
