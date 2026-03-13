# DTOs — Data Transfer Objects

## What Is a DTO?

A **Data Transfer Object** is a simple object used to transfer data between
layers or over the network. DTOs have no behavior — they're just containers
for data, typically implemented as C# records.

## Why Not Just Use Entities?

It's tempting to send domain entities directly in API responses. Here's why
that's a bad idea:

1. **Tight coupling** — Your API contract becomes your database schema. Rename
   a column? Every client breaks.

2. **Over-exposure** — Entities may contain internal state (audit fields, soft
   delete flags, navigation properties) that clients shouldn't see.

3. **Circular references** — Navigation properties (`Column.RetroBoard`,
   `RetroBoard.Columns`) cause infinite loops during JSON serialization.

4. **Security** — In a POST request, you might accidentally bind properties
   the client shouldn't control (e.g., `Id`, `CreatedAt`).

## DTO Patterns in RetroBoard

### Request DTOs

Define the exact shape of what the client sends:

```csharp
// Only the data the client should provide
public record CreateColumnRequest(string Name);

public record UpdateColumnRequest(string Name);

public record CastVoteRequest(Guid UserId);
```

### Response DTOs

Define the exact shape of what the client receives:

```csharp
public record ColumnResponse(Guid Id, string Name, List<NoteResponse>? Notes);

public record NoteResponse(Guid Id, string Text, int? VoteCount);
```

## Mapping: DTOs ↔ Entities

This repository uses **manual mapping** instead of AutoMapper:

```csharp
// In a service or handler
private static ColumnResponse MapToResponse(Column column) =>
    new(column.Id, column.Name, column.Notes?.Select(MapToResponse).ToList());
```

### Why No AutoMapper?

- **Explicitness** — You can see exactly what maps to what. No "magic" conventions.
- **Compile-time safety** — Rename a property and the compiler tells you.
- **Educational clarity** — Students can trace the data flow step by step.
- **No hidden behavior** — AutoMapper profiles can contain logic that's hard to discover.

For a small-to-medium codebase, manual mapping is perfectly fine. AutoMapper
shines when you have dozens of nearly-identical mappings.

## Where DTOs Live

In the RetroBoard project structure, DTOs live in the **Application layer**:

```
Api1.Application/
├── DTOs/
│   ├── Requests/
│   │   ├── CreateColumnRequest.cs
│   │   ├── UpdateColumnRequest.cs
│   │   └── CastVoteRequest.cs
│   └── Responses/
│       ├── ColumnResponse.cs
│       ├── NoteResponse.cs
│       └── VoteResponse.cs
```

This is intentional — the Application layer owns the **use case contract**
(what goes in, what comes out). The Domain layer knows nothing about DTOs,
and the WebApi layer doesn't define its own.

## DTOs vs Rich Domain Models

| Aspect | DTO | Rich Domain Model |
|--------|-----|-------------------|
| Purpose | Data transfer | Business logic enforcement |
| Behavior | None | Methods, invariant checks |
| Setters | Public (or init) | Private |
| Used by | Controllers, clients | Services, handlers |
| Serializable | Yes (JSON) | Not directly |

They serve completely different purposes and should never be confused. A DTO
is a **message**. A domain entity is a **behavioral object**.

## Where to Go Next

- [Services & Orchestration](services.md) — Services bridge the gap between
  DTOs and domain entities.
- [Entities & the Anemic Domain Model](entities.md) — Understand the entity
  side of the mapping.
