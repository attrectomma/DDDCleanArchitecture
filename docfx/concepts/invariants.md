# Domain Exceptions & Invariants

## What Is an Invariant?

An **invariant** is a business rule that must always be true. If an invariant
is violated, the system is in an invalid state.

In the RetroBoard domain, the invariants are:

| Invariant | Scope |
|-----------|-------|
| Column names within a retro must be unique | Cross-entity (retro → columns) |
| Note text within a column must be unique | Cross-entity (column → notes) |
| A user may cast only 1 vote per note | Cross-entity (note → votes) |
| A user must be a project member to participate | Cross-aggregate (project → retro) |

## Where Invariants Are Enforced

| Tier | Within-Entity Rules | Cross-Entity Rules | Cross-Aggregate Rules |
|------|--------------------|--------------------|----------------------|
| API 1 | Service layer | Service layer | Service layer |
| API 2 | **Entity methods** | Service layer | Service layer |
| API 3 | Entity methods | **Aggregate root** | Service layer |
| API 4 | Entity methods | Aggregate root | **Service + DB constraints** |
| API 5 | Entity methods | Aggregate root | **Command handler + DB constraints** |

## Domain Exceptions

When an invariant is violated, the domain throws a domain-specific exception:

```csharp
// Base exception for all domain errors
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// Specific: an invariant was violated
public class InvariantViolationException : DomainException
{
    public InvariantViolationException(string message) : base(message) { }
}

// Specific: entity not found
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, Guid id)
        : base($"{entityName} with ID {id} was not found.") { }
}
```

## Exception-to-HTTP Mapping

The global exception handling middleware maps domain exceptions to HTTP
Problem Details responses:

| Exception | HTTP Status | When |
|-----------|------------|------|
| `NotFoundException` | 404 Not Found | Entity doesn't exist |
| `InvariantViolationException` | 409 Conflict | Business rule violated |
| `DomainException` | 422 Unprocessable Entity | General domain error |
| `ValidationException` | 400 Bad Request | Input validation failure |
| `DbUpdateConcurrencyException` | 409 Conflict | Optimistic concurrency (API 3+) |

## Where to Go Next

- [Consistency Boundaries](consistency-boundaries.md) — How invariant scope
  relates to aggregate design.
- [Aggregates](aggregates.md) — How aggregate roots enforce cross-entity
  invariants.
