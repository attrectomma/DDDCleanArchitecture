# API 3 — Aggregate Design (Detailed Implementation Plan)

> **Theme:** Introduce proper **Aggregate Design** with two aggregates:
> **Project** (owns Members) and **RetroBoard** (owns Columns → Notes → Votes).
> All lower-level repositories and services disappear. Each aggregate has
> one repository and one service (or application-layer handler). Consistency
> boundaries are explicit. Optimistic concurrency is enforced.

---

## 1. What Changes from API 2

| Aspect | API 2 | API 3 |
|--------|-------|-------|
| Repository count | 7 (per entity) | 2 (per aggregate) |
| Service count | 7 | 2 (`ProjectService`, `RetroBoardService`) |
| Invariant enforcement | Entity methods + some in service | 100% in aggregate root methods |
| Concurrency | None | Optimistic via `xmin` / RowVersion |
| Consistency boundary | None | Aggregate root = transaction boundary |
| Trade-off | — | **Aggregate explosion risk** (large RetroBoard) |

> **DESIGN:** The RetroBoard aggregate is intentionally **large** — it contains
> all columns, notes, and votes. This guarantees consistency but creates risks:
> - Loading the full aggregate for a single vote is expensive.
> - Write contention: any two writes to the same retro conflict.
> API 4 addresses this by splitting Vote into its own aggregate.

---

## 2. Project Structure

```
src/Api3.Aggregates/
├── Api3.Domain/
│   ├── Common/
│   │   ├── AuditableEntityBase.cs
│   │   ├── IAggregateRoot.cs              (marker interface)
│   │   ├── Entity.cs                      (base for non-root entities)
│   │   └── Guard.cs
│   ├── ProjectAggregate/
│   │   ├── Project.cs                     (aggregate root)
│   │   ├── ProjectMember.cs               (entity within aggregate)
│   │   └── IProjectRepository.cs          (lives in Domain layer)
│   ├── RetroAggregate/
│   │   ├── RetroBoard.cs                  (aggregate root)
│   │   ├── Column.cs                      (entity)
│   │   ├── Note.cs                        (entity)
│   │   ├── Vote.cs                        (value object / entity)
│   │   └── IRetroBoardRepository.cs
│   ├── UserAggregate/
│   │   ├── User.cs                        (simple aggregate root)
│   │   └── IUserRepository.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs
│   │   └── InvariantViolationException.cs
│   └── Api3.Domain.csproj
│
├── Api3.Application/
│   ├── Services/
│   │   ├── IProjectService.cs / ProjectService.cs
│   │   ├── IRetroBoardService.cs / RetroBoardService.cs
│   │   └── IUserService.cs / UserService.cs
│   ├── DTOs/                              (same as API 1/2)
│   ├── Validators/
│   ├── Exceptions/
│   │   └── NotFoundException.cs
│   └── Api3.Application.csproj
│
├── Api3.Infrastructure/
│   ├── Persistence/
│   │   ├── RetroBoardDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   ├── Configurations/
│   │   │   ├── ProjectConfiguration.cs      (includes ProjectMember)
│   │   │   ├── RetroBoardConfiguration.cs   (includes Column, Note, Vote)
│   │   │   └── UserConfiguration.cs
│   │   ├── Interceptors/
│   │   │   └── AuditInterceptor.cs
│   │   └── Repositories/
│   │       ├── ProjectRepository.cs
│   │       ├── RetroBoardRepository.cs
│   │       └── UserRepository.cs
│   └── Api3.Infrastructure.csproj
│
└── Api3.WebApi/
    ├── Controllers/
    │   ├── UsersController.cs
    │   ├── ProjectsController.cs
    │   └── RetroBoardsController.cs       (handles columns, notes, votes too)
    ├── Middleware/
    │   ├── GlobalExceptionHandlerMiddleware.cs
    │   └── ConcurrencyConflictMiddleware.cs  (NEW)
    ├── Program.cs
    └── Api3.WebApi.csproj
```

**Key structural difference:** No `ColumnRepository`, `NoteRepository`, or `VoteRepository`.
Everything below the aggregate root is accessed through the root.

---

## 3. Aggregate Design

### 3.1 RetroBoard Aggregate Root

```csharp
/// <summary>
/// Aggregate root for a retrospective board. Owns all columns, notes,
/// and votes. All mutations go through this class to ensure invariants.
/// </summary>
/// <remarks>
/// DESIGN: This is a classic DDD aggregate. The RetroBoard is the
/// consistency boundary — loading it gives us a complete, consistent
/// snapshot that we can validate against.
///
/// TRADE-OFF: This aggregate is potentially LARGE. A retro with 5 columns,
/// 50 notes, and 200 votes means loading ~255 entities for every operation.
/// Furthermore, any concurrent write to the same retro causes a concurrency
/// conflict (optimistic locking on the aggregate root's version).
/// API 4 addresses this by extracting Vote into its own aggregate.
/// </remarks>
public class RetroBoard : AuditableEntityBase, IAggregateRoot
{
    private readonly List<Column> _columns = new();

    private RetroBoard() { }

    public RetroBoard(Guid projectId, string name)
    {
        ProjectId = projectId;
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public uint Version { get; private set; }  // optimistic concurrency (mapped to xmin)
    public IReadOnlyCollection<Column> Columns => _columns.AsReadOnly();

    // ── Column operations ───────────────────────────────────────

    /// <summary>
    /// Adds a column to this retro board.
    /// Enforces: column names must be unique within the board.
    /// </summary>
    public Column AddColumn(string name)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        if (_columns.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"Column name '{name}' already exists in retro '{Name}'.");

        var column = new Column(Id, name);
        _columns.Add(column);
        return column;
    }

    /// <summary>Renames a column, enforcing uniqueness.</summary>
    public void RenameColumn(Guid columnId, string newName)
    {
        var column = GetColumnOrThrow(columnId);

        if (_columns.Any(c => c.Id != columnId &&
            c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"Column name '{newName}' already exists in retro '{Name}'.");

        column.Rename(newName);
    }

    /// <summary>Removes a column (soft delete).</summary>
    public void RemoveColumn(Guid columnId)
    {
        var column = GetColumnOrThrow(columnId);
        _columns.Remove(column);
    }

    // ── Note operations ─────────────────────────────────────────

    /// <summary>Adds a note to a specific column.</summary>
    public Note AddNote(Guid columnId, string text)
    {
        var column = GetColumnOrThrow(columnId);
        return column.AddNote(text);  // Column enforces note uniqueness
    }

    public void UpdateNote(Guid columnId, Guid noteId, string newText)
    {
        var column = GetColumnOrThrow(columnId);
        column.UpdateNote(noteId, newText);
    }

    public void RemoveNote(Guid columnId, Guid noteId)
    {
        var column = GetColumnOrThrow(columnId);
        column.RemoveNote(noteId);
    }

    // ── Vote operations ─────────────────────────────────────────

    /// <summary>
    /// Casts a vote on a note.
    /// DESIGN: The entire aggregate is locked during this operation,
    /// meaning two users voting on DIFFERENT notes in the same retro
    /// will conflict. This is the "aggregate explosion" problem.
    /// API 4 extracts Vote as its own aggregate to avoid this.
    /// </summary>
    public Vote CastVote(Guid columnId, Guid noteId, Guid userId)
    {
        var column = GetColumnOrThrow(columnId);
        var note = column.GetNoteOrThrow(noteId);
        return note.CastVote(userId);
    }

    public void RemoveVote(Guid columnId, Guid noteId, Guid voteId)
    {
        var column = GetColumnOrThrow(columnId);
        var note = column.GetNoteOrThrow(noteId);
        note.RemoveVote(voteId);
    }

    // ── Private helpers ─────────────────────────────────────────

    private Column GetColumnOrThrow(Guid columnId) =>
        _columns.FirstOrDefault(c => c.Id == columnId)
        ?? throw new DomainException($"Column {columnId} not found in retro {Id}.");
}
```

### 3.2 Project Aggregate Root

```csharp
/// <summary>
/// Aggregate root for a project. Owns project membership.
/// </summary>
public class Project : AuditableEntityBase, IAggregateRoot
{
    private readonly List<ProjectMember> _members = new();

    private Project() { }

    public Project(string name)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    public string Name { get; private set; } = string.Empty;
    public uint Version { get; private set; }
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    public ProjectMember AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvariantViolationException($"User {userId} is already a member.");

        var member = new ProjectMember(Id, userId);
        _members.Add(member);
        return member;
    }

    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException($"User {userId} is not a member.");
        _members.Remove(member);
    }

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
}
```

---

## 4. Repository Interfaces (in Domain layer)

```csharp
/// <summary>
/// Repository for the RetroBoard aggregate. Loads/saves the
/// ENTIRE aggregate (board + columns + notes + votes).
/// </summary>
/// <remarks>
/// DESIGN: There is no IColumnRepository or INoteRepository.
/// All child entities are reached through the aggregate root.
/// The repository always loads the complete aggregate graph.
/// </remarks>
public interface IRetroBoardRepository
{
    Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RetroBoard board, CancellationToken ct = default);
    void Delete(RetroBoard board);
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Project project, CancellationToken ct = default);
}
```

### Repository Implementation

```csharp
/// <summary>
/// Loads the full RetroBoard aggregate in a single query with
/// eager loading of all child entities.
/// </summary>
/// <remarks>
/// DESIGN: We always load the complete aggregate because the
/// aggregate root needs the full state to enforce invariants.
/// This is expensive for large retros — a known trade-off at this tier.
///
/// DESIGN (CQRS foreshadowing): This same expensive query runs for
/// BOTH writes (where the full state is needed for invariants) AND
/// reads (where we only need a DTO). Loading the full aggregate graph
/// with change tracking just to map it to a response is wasteful.
/// API 5 introduces CQRS to separate the read path (lightweight
/// no-tracking projections) from the write path (full aggregate loading).
/// </remarks>
public class RetroBoardRepository : IRetroBoardRepository
{
    public async Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
                    .ThenInclude(n => n.Votes)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }
}
```

---

## 5. Application Services (drastically simplified)

```csharp
/// <summary>
/// Application service for all retro board operations.
/// Replaces ColumnService, NoteService, and VoteService from API 1/2.
/// </summary>
/// <remarks>
/// DESIGN: This service is a thin orchestrator:
///   1. Load aggregate
///   2. Call aggregate method (which enforces invariants)
///   3. Save via UoW
/// All business logic lives in the aggregate root.
///
/// DESIGN (CQRS foreshadowing): Notice that GET operations (not shown
/// here) also load the full aggregate via the repository, even though
/// they only need a read-only view. This means read-heavy traffic pays
/// the same cost as writes. API 5 addresses this with CQRS — queries
/// bypass the aggregate and project directly from the database.
/// </remarks>
public class RetroBoardService : IRetroBoardService
{
    private readonly IRetroBoardRepository _repository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<ColumnResponse> AddColumnAsync(
        Guid retroBoardId, CreateColumnRequest request, CancellationToken ct)
    {
        var retro = await _repository.GetByIdAsync(retroBoardId, ct)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // All invariant checking happens inside the aggregate
        var column = retro.AddColumn(request.Name);

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToResponse(column);
    }

    public async Task<VoteResponse> CastVoteAsync(
        Guid retroBoardId, Guid columnId, Guid noteId,
        CastVoteRequest request, CancellationToken ct)
    {
        var retro = await _repository.GetByIdAsync(retroBoardId, ct)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        var vote = retro.CastVote(columnId, noteId, request.UserId);

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToResponse(vote);
    }
}
```

---

## 6. Concurrency Control

### EF Core Configuration

```csharp
/// <summary>
/// Configures RetroBoard as an aggregate root with optimistic concurrency
/// via PostgreSQL's system column <c>xmin</c>.
/// </summary>
public class RetroBoardConfiguration : IEntityTypeConfiguration<RetroBoard>
{
    public void Configure(EntityTypeBuilder<RetroBoard> builder)
    {
        builder.HasKey(r => r.Id);

        // DESIGN: xmin is a PostgreSQL system column that changes on every
        // row update. Using it as a concurrency token means that if two
        // requests load the same retro, the second SaveChanges will throw
        // DbUpdateConcurrencyException. This is how we enforce the
        // aggregate as a consistency boundary.
        builder.UseXminAsConcurrencyToken();

        builder.HasMany(r => r.Columns)
            .WithOne()
            .HasForeignKey(c => c.RetroBoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
```

### Handling Conflicts

```csharp
/// <summary>
/// Middleware that catches <see cref="DbUpdateConcurrencyException"/>
/// and returns HTTP 409 Conflict with a meaningful message.
/// </summary>
public class ConcurrencyConflictMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateConcurrencyException)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = 409,
                Title = "Concurrency conflict",
                Detail = "The resource was modified by another request. Please retry."
            });
        }
    }
}
```

---

## 7. Controllers

Fewer controllers — the RetroBoard controller handles columns, notes, and votes:

```csharp
/// <summary>
/// Controller for all retro board operations including columns, notes, and votes.
/// </summary>
/// <remarks>
/// DESIGN: In API 1/2 there were separate controllers for each entity.
/// With aggregate design, all operations on the RetroBoard aggregate
/// flow through a single entry point. The URL structure still exposes
/// nested resources for REST clarity, but internally they all go to
/// <see cref="IRetroBoardService"/>.
/// </remarks>
[ApiController]
[Route("api/projects/{projectId:guid}/retros")]
public class RetroBoardsController : ControllerBase
{
    [HttpPost("{retroId:guid}/columns")]
    public async Task<IActionResult> AddColumn(Guid retroId, CreateColumnRequest request, CancellationToken ct)
    {
        var column = await _service.AddColumnAsync(retroId, request, ct);
        return CreatedAtAction(/* ... */);
    }

    [HttpPost("{retroId:guid}/columns/{columnId:guid}/notes/{noteId:guid}/votes")]
    public async Task<IActionResult> CastVote(
        Guid retroId, Guid columnId, Guid noteId,
        CastVoteRequest request, CancellationToken ct)
    {
        var vote = await _service.CastVoteAsync(retroId, columnId, noteId, request, ct);
        return CreatedAtAction(/* ... */);
    }
}
```

---

## 8. What This Tier Solves vs API 2

| Problem | Solved? | How |
|---------|---------|-----|
| Race conditions on uniqueness | ✅ | Aggregate loaded + xmin ensures atomic read-modify-write |
| No optimistic concurrency | ✅ | `UseXminAsConcurrencyToken()` |
| Scattered business logic | ✅ | All logic in aggregate root |
| Loading strategy coupling | ✅ | Aggregate repo always loads full graph |

---

## 9. New Problems Introduced

| Problem | Description |
|---------|------------|
| **Aggregate explosion** | A retro with many columns/notes/votes → huge object graph per load |
| **Write contention** | Voting on note A conflicts with voting on note B (same retro → same aggregate) |
| **Performance** | Every vote operation loads ALL columns/notes/votes just to add one vote |

> These are addressed in API 4 by extracting Vote as a separate aggregate.

---

## 10. Files to Create

| Layer | Files |
|-------|-------|
| Domain | `IAggregateRoot.cs`, `Entity.cs`, `RetroBoard.cs` (full aggregate root), `Column.cs`, `Note.cs`, `Vote.cs`, `Project.cs`, `ProjectMember.cs`, `User.cs`, `IRetroBoardRepository.cs`, `IProjectRepository.cs`, `IUserRepository.cs` |
| Application | `RetroBoardService.cs`, `ProjectService.cs`, `UserService.cs` (3 services total) |
| Infrastructure | `RetroBoardRepository.cs`, `ProjectRepository.cs`, `UserRepository.cs`, configurations, interceptors |
| WebApi | `RetroBoardsController.cs`, `ProjectsController.cs`, `UsersController.cs`, concurrency middleware |
