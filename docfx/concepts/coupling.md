# Coupling & Dependency Management

## What Is Coupling?

**Coupling** measures how much one module depends on another. High coupling
means a change in one module forces changes in others. Low coupling means
modules can evolve independently.

In software architecture, we want:
- **Low coupling** between layers and modules
- **High cohesion** within a module (things that change together, live together)

## Types of Coupling in This Repository

### 1. Layer Coupling (Dependency Direction)

Clean Architecture enforces a strict dependency rule:

```
WebApi → Application → Domain
                ↑
         Infrastructure
```

- **Domain** depends on nothing.
- **Application** depends on Domain.
- **Infrastructure** depends on Domain (and optionally Application).
- **WebApi** depends on Application (and indirectly on Domain).

Infrastructure implements interfaces defined in Domain/Application, following
the **Dependency Inversion Principle**.

### 2. Loading Strategy Coupling (API 2)

In API 2, the `Column.AddNote()` method checks for duplicate note text in its
`_notes` collection. But this only works if the `Notes` collection was loaded:

```csharp
// If the service forgot to Include(c => c.Notes), this list is empty
// and the duplicate check silently passes!
var column = await _context.Columns
    .FirstOrDefaultAsync(c => c.Id == columnId);  // Notes NOT loaded

column.AddNote("Duplicate text");  // No exception! The _notes list is empty.
```

This is **implicit coupling** between the loading strategy (what the
repository includes) and the domain logic (what the entity checks).

**API 3 solves this** by always loading the complete aggregate through the
repository. The aggregate root's repository knows what to include.

### 3. Service-to-Service Coupling (API 1)

In API 1, services sometimes need to call each other or share repository
dependencies:

```csharp
// VoteService needs to check if a note exists → depends on INoteRepository
// VoteService needs to check if user is a member → depends on IProjectMemberRepository
public class VoteService : IVoteService
{
    private readonly IVoteRepository _voteRepository;
    private readonly INoteRepository _noteRepository;        // cross-entity dependency
    private readonly IProjectMemberRepository _memberRepo;   // another cross-entity dependency
}
```

This creates a web of dependencies. **API 3+ reduces this** by consolidating
related operations under a single aggregate service.

### 4. Controller-to-Service Coupling (API 1–4 vs 5)

In API 1–4, controllers depend on specific service interfaces:

```csharp
// Tightly coupled — controller knows about a specific service
public class ColumnsController : ControllerBase
{
    private readonly IColumnService _columnService;
}
```

In API 5, controllers depend only on `IMediator`:

```csharp
// Loosely coupled — controller doesn't know which handler will execute
public class ColumnsController : ControllerBase
{
    private readonly IMediator _mediator;  // single dependency, any command/query
}
```

## Measuring Improvement

| Coupling Type | API 1 | API 2 | API 3 | API 4 | API 5 |
|--------------|-------|-------|-------|-------|-------|
| Service dependencies | 7 repos + 7 services | Same | 3 repos + 3 services | 4 each | 4 repos + IMediator |
| Loading strategy | N/A | ⚠️ Implicit | ✅ Repo loads full aggregate | ✅ Same | ✅ Same |
| Cross-entity coordination | Multiple services | Same | Single aggregate service | 2 aggregate services | Command handlers |
| Controller coupling | Per-service | Per-service | Per-aggregate service | Per-aggregate service | **IMediator only** |

## Where to Go Next

- [Repositories](repositories.md) — How repository design affects coupling.
- [Services & Orchestration](services.md) — Service coupling patterns.
