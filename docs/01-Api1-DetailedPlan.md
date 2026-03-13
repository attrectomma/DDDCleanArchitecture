# API 1 — Anemic CRUD (Detailed Implementation Plan)

> **Theme:** Table → Entity → Repository → Service → Controller (1-to-1 mapping).
> Business logic lives entirely in the Service layer. Domain models are **anemic** (property bags).
> No awareness of consistency boundaries or concurrency control.

---

## 1. Project Structure

```
src/Api1.AnemicCrud/
├── Api1.Domain/
│   ├── Entities/
│   │   ├── AuditableEntityBase.cs
│   │   ├── User.cs
│   │   ├── Project.cs
│   │   ├── ProjectMember.cs       (join entity)
│   │   ├── RetroBoard.cs
│   │   ├── Column.cs
│   │   ├── Note.cs
│   │   └── Vote.cs
│   └── Api1.Domain.csproj
│
├── Api1.Application/
│   ├── Interfaces/
│   │   ├── IUnitOfWork.cs
│   │   ├── IUserRepository.cs
│   │   ├── IProjectRepository.cs
│   │   ├── IProjectMemberRepository.cs
│   │   ├── IRetroBoardRepository.cs
│   │   ├── IColumnRepository.cs
│   │   ├── INoteRepository.cs
│   │   └── IVoteRepository.cs
│   ├── Services/
│   │   ├── IUserService.cs / UserService.cs
│   │   ├── IProjectService.cs / ProjectService.cs
│   │   ├── IProjectMemberService.cs / ProjectMemberService.cs
│   │   ├── IRetroBoardService.cs / RetroBoardService.cs
│   │   ├── IColumnService.cs / ColumnService.cs
│   │   ├── INoteService.cs / NoteService.cs
│   │   └── IVoteService.cs / VoteService.cs
│   ├── DTOs/
│   │   ├── Requests/
│   │   │   ├── CreateUserRequest.cs
│   │   │   ├── CreateProjectRequest.cs
│   │   │   ├── AddMemberRequest.cs
│   │   │   ├── CreateRetroBoardRequest.cs
│   │   │   ├── CreateColumnRequest.cs
│   │   │   ├── UpdateColumnRequest.cs
│   │   │   ├── CreateNoteRequest.cs
│   │   │   ├── UpdateNoteRequest.cs
│   │   │   └── CastVoteRequest.cs
│   │   └── Responses/
│   │       ├── UserResponse.cs
│   │       ├── ProjectResponse.cs
│   │       ├── RetroBoardResponse.cs
│   │       ├── ColumnResponse.cs
│   │       ├── NoteResponse.cs
│   │       └── VoteResponse.cs
│   ├── Validators/                 (FluentValidation)
│   │   ├── CreateUserRequestValidator.cs
│   │   ├── CreateProjectRequestValidator.cs
│   │   └── ...
│   ├── Exceptions/
│   │   ├── NotFoundException.cs
│   │   ├── DuplicateException.cs
│   │   └── BusinessRuleException.cs
│   └── Api1.Application.csproj
│
├── Api1.Infrastructure/
│   ├── Persistence/
│   │   ├── RetroBoardDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   ├── Configurations/        (IEntityTypeConfiguration per entity)
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── ProjectConfiguration.cs
│   │   │   ├── ProjectMemberConfiguration.cs
│   │   │   ├── RetroBoardConfiguration.cs
│   │   │   ├── ColumnConfiguration.cs
│   │   │   ├── NoteConfiguration.cs
│   │   │   └── VoteConfiguration.cs
│   │   ├── Interceptors/
│   │   │   ├── AuditInterceptor.cs
│   │   │   └── SoftDeleteInterceptor.cs
│   │   └── Repositories/
│   │       ├── UserRepository.cs
│   │       ├── ProjectRepository.cs
│   │       ├── ProjectMemberRepository.cs
│   │       ├── RetroBoardRepository.cs
│   │       ├── ColumnRepository.cs
│   │       ├── NoteRepository.cs
│   │       └── VoteRepository.cs
│   └── Api1.Infrastructure.csproj
│
└── Api1.WebApi/
    ├── Controllers/
    │   ├── UsersController.cs
    │   ├── ProjectsController.cs
    │   ├── RetroBoardsController.cs
    │   ├── ColumnsController.cs
    │   ├── NotesController.cs
    │   └── VotesController.cs
    ├── Middleware/
    │   └── GlobalExceptionHandlerMiddleware.cs
    ├── Program.cs
    ├── appsettings.json
    └── Api1.WebApi.csproj
```

---

## 2. Entity Design (Anemic)

All entities are **pure property bags** — no methods, no invariant enforcement.

```csharp
/// <summary>
/// Base class for all auditable entities. Provides tracking for creation,
/// modification, and soft-delete timestamps.
/// </summary>
/// <remarks>
/// DESIGN: Timestamps are populated by <see cref="AuditInterceptor"/> so
/// entities remain unaware of auditing — this is intentional in the anemic
/// model where entities carry no behaviour.
/// </remarks>
public abstract class AuditableEntityBase
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}

/// <summary>Represents a user that can participate in retro boards.</summary>
public class User : AuditableEntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}

/// <summary>Represents a retro column (e.g., "What went well").</summary>
public class Column : AuditableEntityBase
{
    public Guid RetroBoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public RetroBoard RetroBoard { get; set; } = null!;
    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
// ... same pattern for all other entities
```

### Key point for teaching

> **DESIGN COMMENT:** In API 1 these entities are *anemic* — they expose
> public setters and contain zero business logic. All rules (unique column
> names, one-vote-per-user) are enforced in the Service layer.
> See API 2 for the contrast where logic moves into the domain.

---

## 3. Repository Layer

One repository per entity, each following the same interface template:

```csharp
/// <summary>Generic repository contract for <typeparamref name="T"/>.</summary>
public interface IRepository<T> where T : AuditableEntityBase
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Delete(T entity);  // sets DeletedAt — soft delete
}
```

Entity-specific interfaces extend this with query methods:

```csharp
public interface IColumnRepository : IRepository<Column>
{
    Task<bool> ExistsByNameInRetroAsync(Guid retroBoardId, string name, CancellationToken ct = default);
    Task<List<Column>> GetByRetroBoardIdAsync(Guid retroBoardId, CancellationToken ct = default);
}
```

### Implementation

- Generic `RepositoryBase<T>` using `RetroBoardDbContext`.
- Global query filter for soft delete: `.HasQueryFilter(e => e.DeletedAt == null)`.

---

## 4. Service Layer (where all business logic lives)

Each service maps 1-to-1 to a controller action set.

```csharp
/// <summary>
/// Service responsible for all Column-related business logic.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 all invariant checks live here in the service layer.
/// The domain entity <see cref="Column"/> is a plain DTO with no behaviour.
/// This is the "anemic domain model" anti-pattern — common in junior codebases
/// but problematic because business rules are scattered across services.
/// See API 2 where these checks move into the entity itself.
/// </remarks>
public class ColumnService : IColumnService
{
    public async Task<ColumnResponse> CreateAsync(Guid retroBoardId, CreateColumnRequest request, CancellationToken ct)
    {
        // 1. Verify retro board exists
        var retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, ct)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // 2. INVARIANT: column name must be unique within retro
        //    DESIGN: This check is NOT atomic — a race condition can
        //    create duplicates. API 3 solves this with aggregate locking.
        if (await _columnRepository.ExistsByNameInRetroAsync(retroBoardId, request.Name, ct))
            throw new DuplicateException("Column", "Name", request.Name);

        // 3. Map & persist
        var column = new Column { RetroBoardId = retroBoardId, Name = request.Name };
        await _columnRepository.AddAsync(column, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToResponse(column);
    }
}
```

### VoteService — uniqueness check

```csharp
/// <summary>
/// INVARIANT: A user may cast only 1 vote per note.
/// DESIGN: Checked via a query before insert — NOT safe under concurrency.
/// API 3+ enforce this inside the aggregate boundary with a DB unique constraint.
/// </summary>
public async Task<VoteResponse> CastVoteAsync(Guid noteId, CastVoteRequest request, CancellationToken ct)
{
    if (await _voteRepository.ExistsAsync(noteId, request.UserId, ct))
        throw new BusinessRuleException("User has already voted on this note.");
    // ...
}
```

---

## 5. Infrastructure — EF Core

### DbContext

```csharp
public class RetroBoardDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<RetroBoard> RetroBoards => Set<RetroBoard>();
    public DbSet<Column> Columns => Set<Column>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Vote> Votes => Set<Vote>();
}
```

### Entity Configurations

- `VoteConfiguration`: Unique index on `(NoteId, UserId)` — DB-level safety net.
- `ColumnConfiguration`: Unique index on `(RetroBoardId, Name)`.
- `NoteConfiguration`: Unique index on `(ColumnId, Text)`.
- All configs apply `.HasQueryFilter(e => e.DeletedAt == null)`.

### Interceptors

```csharp
/// <summary>
/// EF Core interceptor that stamps CreatedAt / LastUpdatedAt on save,
/// and converts Delete operations into soft deletes by setting DeletedAt.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct)
    {
        foreach (var entry in eventData.Context!.ChangeTracker.Entries<AuditableEntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.LastUpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastUpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }
        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

### Unit of Work

```csharp
/// <summary>
/// Wraps DbContext.SaveChangesAsync to provide an explicit unit-of-work boundary.
/// </summary>
/// <remarks>
/// DESIGN: In API 1, each service calls UoW.SaveChangesAsync() at the end
/// of its method. There is no transactional coordination across services —
/// a deliberate simplification (and weakness) at this tier.
/// </remarks>
public class UnitOfWork : IUnitOfWork
{
    private readonly RetroBoardDbContext _context;
    public Task<int> SaveChangesAsync(CancellationToken ct) => _context.SaveChangesAsync(ct);
}
```

---

## 6. Controllers

Thin controllers that delegate to the service layer:

```csharp
[ApiController]
[Route("api/retros/{retroId:guid}/columns")]
public class ColumnsController : ControllerBase
{
    private readonly IColumnService _columnService;

    [HttpPost]
    [ProducesResponseType(typeof(ColumnResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(Guid retroId, CreateColumnRequest request, CancellationToken ct)
    {
        var response = await _columnService.CreateAsync(retroId, request, ct);
        return CreatedAtAction(nameof(GetById), new { retroId, columnId = response.Id }, response);
    }
}
```

---

## 7. Known Weaknesses (documented intentionally)

| Weakness | Explanation |
|----------|------------|
| **Race conditions on uniqueness checks** | Check-then-act in service is not atomic. Two concurrent requests can both pass the "exists?" check. |
| **No optimistic concurrency** | No `ConcurrencyToken` / `RowVersion`. Last-write-wins silently. |
| **Scattered business logic** | Rules live across 6+ service classes. Adding a cross-cutting rule means touching multiple services. |
| **Anemic domain** | Entities are dumb data holders — the "domain" layer adds no value beyond defining shapes. |
| **No aggregate boundary** | Every entity is independently loadable and saveable. There's no single place to ensure a retro board's invariants hold. |

> These weaknesses are **intentional** for teaching purposes. Each subsequent API tier addresses one or more of them.

---

## 8. DI Registration (Program.cs outline)

```csharp
builder.Services.AddDbContext<RetroBoardDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("RetroBoard")));

// Interceptors
builder.Services.AddSingleton<AuditInterceptor>();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
builder.Services.AddScoped<IRetroBoardRepository, RetroBoardRepository>();
builder.Services.AddScoped<IColumnRepository, ColumnRepository>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();

// Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IProjectMemberService, ProjectMemberService>();
builder.Services.AddScoped<IRetroBoardService, RetroBoardService>();
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IVoteService, VoteService>();

// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

---

## 9. Files to Create (ordered)

1. `Api1.Domain.csproj` + all entity classes
2. `Api1.Application.csproj` + interfaces, services, DTOs, validators, exceptions
3. `Api1.Infrastructure.csproj` + DbContext, configurations, interceptors, repositories, UoW
4. `Api1.WebApi.csproj` + controllers, middleware, Program.cs, appsettings
5. EF Core migration (initial)
