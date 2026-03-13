# API 4 — Split Aggregates (Detailed Implementation Plan)

> **Theme:** Extract **Vote** as its own aggregate to reduce write contention
> and aggregate size. The RetroBoard aggregate shrinks, but we now need
> **cross-aggregate invariant checks** (e.g., "user can vote only once per note"
> must be enforced without loading the full retro).

---

## 1. What Changes from API 3

| Aspect | API 3 | API 4 |
|--------|-------|-------|
| Aggregates | 3 (User, Project, RetroBoard) | 4 (User, Project, RetroBoard, **Vote**) |
| RetroBoard contains | Columns → Notes → Votes | Columns → Notes (no votes) |
| Vote aggregate | Part of RetroBoard | **Standalone** — `Vote` is its own aggregate root |
| Write contention on voting | High (locks entire retro) | Low (locks only the vote row) |
| Cross-aggregate checks | Not needed | **Required** (note exists? user is member? already voted?) |
| Repository count | 3 | 4 |
| Service count | 3 | 4 (+ `VoteService`) |

> **DESIGN:** Splitting an aggregate always involves a trade-off:
> - **Pro:** Better write scalability, smaller transaction scope, less contention.
> - **Con:** Cross-aggregate invariants can't be enforced atomically. We rely on
>   a combination of DB unique constraints and application-level checks.

---

## 2. Project Structure

```
src/Api4.SplitAggregates/
├── Api4.Domain/
│   ├── Common/
│   │   ├── AuditableEntityBase.cs
│   │   ├── IAggregateRoot.cs
│   │   ├── Entity.cs
│   │   └── Guard.cs
│   ├── UserAggregate/
│   │   ├── User.cs
│   │   └── IUserRepository.cs
│   ├── ProjectAggregate/
│   │   ├── Project.cs
│   │   ├── ProjectMember.cs
│   │   └── IProjectRepository.cs
│   ├── RetroAggregate/
│   │   ├── RetroBoard.cs                  (NO votes inside)
│   │   ├── Column.cs
│   │   ├── Note.cs
│   │   └── IRetroBoardRepository.cs
│   ├── VoteAggregate/                     ← NEW
│   │   ├── Vote.cs                        (aggregate root)
│   │   └── IVoteRepository.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs
│   │   └── InvariantViolationException.cs
│   └── Api4.Domain.csproj
│
├── Api4.Application/
│   ├── Services/
│   │   ├── IUserService.cs / UserService.cs
│   │   ├── IProjectService.cs / ProjectService.cs
│   │   ├── IRetroBoardService.cs / RetroBoardService.cs
│   │   └── IVoteService.cs / VoteService.cs     ← NEW
│   ├── DTOs/
│   ├── Validators/
│   └── Api4.Application.csproj
│
├── Api4.Infrastructure/
│   ├── Persistence/
│   │   ├── RetroBoardDbContext.cs
│   │   ├── UnitOfWork.cs
│   │   ├── Configurations/
│   │   │   ├── UserConfiguration.cs
│   │   │   ├── ProjectConfiguration.cs
│   │   │   ├── RetroBoardConfiguration.cs
│   │   │   ├── NoteConfiguration.cs
│   │   │   └── VoteConfiguration.cs          (own table, unique index)
│   │   ├── Interceptors/
│   │   │   └── AuditInterceptor.cs
│   │   └── Repositories/
│   │       ├── UserRepository.cs
│   │       ├── ProjectRepository.cs
│   │       ├── RetroBoardRepository.cs       (no longer loads votes)
│   │       └── VoteRepository.cs             ← NEW
│   └── Api4.Infrastructure.csproj
│
└── Api4.WebApi/
    ├── Controllers/
    │   ├── UsersController.cs
    │   ├── ProjectsController.cs
    │   ├── RetroBoardsController.cs
    │   └── VotesController.cs                ← NEW (or nested in notes)
    ├── Middleware/
    │   ├── GlobalExceptionHandlerMiddleware.cs
    │   └── ConcurrencyConflictMiddleware.cs
    ├── Program.cs
    └── Api4.WebApi.csproj
```

---

## 3. Aggregate Changes

### 3.1 RetroBoard (slimmed down)

```csharp
/// <summary>
/// Aggregate root for a retrospective board. Owns columns and notes,
/// but NOT votes (votes are their own aggregate in API 4).
/// </summary>
/// <remarks>
/// DESIGN: Compared to API 3, we removed Vote from this aggregate.
/// Benefits:
///   - Loading a retro no longer pulls in potentially hundreds of votes.
///   - Voting doesn't lock the entire retro — only the Vote aggregate.
///   - Two users can vote concurrently on different notes without conflict.
///
/// Cost:
///   - We can no longer enforce "one vote per user per note" inside this aggregate.
///   - That invariant moves to a DB unique constraint + application-level check.
/// </remarks>
public class RetroBoard : AuditableEntityBase, IAggregateRoot
{
    private readonly List<Column> _columns = new();

    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public uint Version { get; private set; }
    public IReadOnlyCollection<Column> Columns => _columns.AsReadOnly();

    // Column operations — same as API 3
    public Column AddColumn(string name) { /* ... */ }
    public void RenameColumn(Guid columnId, string newName) { /* ... */ }
    public void RemoveColumn(Guid columnId) { /* ... */ }

    // Note operations — same as API 3
    public Note AddNote(Guid columnId, string text) { /* ... */ }
    public void UpdateNote(Guid columnId, Guid noteId, string newText) { /* ... */ }
    public void RemoveNote(Guid columnId, Guid noteId) { /* ... */ }

    // ❌ NO vote operations — votes are a separate aggregate
}
```

### 3.2 Note (no longer holds votes)

```csharp
/// <summary>
/// A sticky note within a column. Does NOT contain votes in API 4.
/// </summary>
/// <remarks>
/// DESIGN: In API 3, Note had a Votes collection and enforced the
/// one-vote-per-user invariant. In API 4, Vote is its own aggregate,
/// so Note is unaware of votes entirely. The vote uniqueness constraint
/// is enforced by the Vote aggregate + a DB unique index.
/// </remarks>
public class Note : Entity
{
    public Guid ColumnId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    // No Votes collection
}
```

### 3.3 Vote (new aggregate root)

```csharp
/// <summary>
/// Aggregate root representing a single vote on a note by a user.
/// </summary>
/// <remarks>
/// DESIGN: Extracting Vote as its own aggregate means:
///   - Each vote is an independent unit of consistency.
///   - Voting on note A doesn't conflict with voting on note B.
///   - The "one vote per user per note" invariant is enforced by:
///     1. A unique index on (NoteId, UserId) in the database.
///     2. An application-level check in VoteService before creation.
///   - This is an **eventual consistency** trade-off: the app-level check
///     can race, but the DB constraint provides the ultimate safety net.
///
/// TRADE-OFF: If the DB constraint catches a duplicate, we must handle
/// the DbUpdateException and return a meaningful error, not a 500.
/// </remarks>
public class Vote : AuditableEntityBase, IAggregateRoot
{
    private Vote() { }

    /// <summary>
    /// Factory method to create a vote.
    /// </summary>
    /// <param name="noteId">The note being voted on.</param>
    /// <param name="userId">The user casting the vote.</param>
    public Vote(Guid noteId, Guid userId)
    {
        NoteId = noteId;
        UserId = userId;
    }

    public Guid NoteId { get; private set; }
    public Guid UserId { get; private set; }
    public uint Version { get; private set; }
}
```

---

## 4. Vote Repository

```csharp
/// <summary>
/// Repository for the Vote aggregate.
/// </summary>
public interface IVoteRepository
{
    Task<Vote?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid noteId, Guid userId, CancellationToken ct = default);
    Task AddAsync(Vote vote, CancellationToken ct = default);
    void Delete(Vote vote);
    Task<List<Vote>> GetByNoteIdAsync(Guid noteId, CancellationToken ct = default);
}
```

---

## 5. VoteService (cross-aggregate coordination)

```csharp
/// <summary>
/// Application service for vote operations. Coordinates across
/// the Vote and RetroBoard aggregates.
/// </summary>
/// <remarks>
/// DESIGN: This is the key difference from API 3. Voting now requires:
///   1. Verify the note exists (query RetroBoard or Note table).
///   2. Verify the user is a project member (query Project aggregate).
///   3. Check for existing vote (query Vote aggregate or rely on DB constraint).
///   4. Create and persist the Vote aggregate.
///
/// Cross-aggregate invariants:
///   - "Note must exist" → checked via read query (not transactionally safe
///     if the note is deleted concurrently, but acceptable — the FK constraint
///     on the Vote table would catch it).
///   - "User is project member" → checked via Project aggregate read.
///   - "One vote per user per note" → checked in app + DB unique constraint.
///
/// IMPORTANT: Steps 1-3 are READ operations on OTHER aggregates. They are
/// NOT in the same transaction as step 4. This is the cost of splitting
/// aggregates — we lose transactional consistency across them.
/// </remarks>
public class VoteService : IVoteService
{
    private readonly IVoteRepository _voteRepository;
    private readonly IRetroBoardRepository _retroRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<VoteResponse> CastVoteAsync(
        Guid noteId, CastVoteRequest request, CancellationToken ct)
    {
        // Cross-aggregate check 1: Does the note exist?
        // DESIGN: We could query the Note table directly (read model) or
        // load the RetroBoard aggregate. Loading the full aggregate just to
        // check note existence would be wasteful. Instead, we use a
        // lightweight query.
        var noteExists = await _retroRepository.NoteExistsAsync(noteId, ct);
        if (!noteExists)
            throw new NotFoundException("Note", noteId);

        // Cross-aggregate check 2: Already voted?
        // DESIGN: This is a "best effort" check. Under high concurrency,
        // two requests could both pass this check. The DB unique constraint
        // on (NoteId, UserId) is the real safety net.
        if (await _voteRepository.ExistsAsync(noteId, request.UserId, ct))
            throw new InvariantViolationException(
                $"User {request.UserId} has already voted on note {noteId}.");

        var vote = new Vote(noteId, request.UserId);
        await _voteRepository.AddAsync(vote, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx
            && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // DESIGN: DB constraint caught a race condition.
            throw new InvariantViolationException(
                $"User {request.UserId} has already voted on note {noteId}.");
        }

        return MapToResponse(vote);
    }

    public async Task RemoveVoteAsync(Guid noteId, Guid voteId, CancellationToken ct)
    {
        var vote = await _voteRepository.GetByIdAsync(voteId, ct)
            ?? throw new NotFoundException("Vote", voteId);

        _voteRepository.Delete(vote);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

---

## 6. RetroBoardRepository Changes

The repository no longer eager-loads votes:

```csharp
/// <summary>
/// Loads the RetroBoard aggregate WITHOUT votes (votes are separate).
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 3's repository that loaded
/// .ThenInclude(n => n.Votes). Removing that ThenInclude means:
///   - Faster aggregate loading.
///   - Smaller memory footprint.
///   - No write contention between note edits and vote operations.
/// </remarks>
public class RetroBoardRepository : IRetroBoardRepository
{
    public async Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
            // NO .ThenInclude(n => n.Votes) — votes are a separate aggregate
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    /// <summary>
    /// Lightweight check: does a note with this ID exist?
    /// Used by VoteService for cross-aggregate validation.
    /// </summary>
    public async Task<bool> NoteExistsAsync(Guid noteId, CancellationToken ct)
    {
        return await _context.Set<Note>()
            .AnyAsync(n => n.Id == noteId && n.DeletedAt == null, ct);
    }
}
```

---

## 7. DB Configuration for Vote

```csharp
/// <summary>
/// Configures Vote as a standalone aggregate root with its own
/// concurrency token and a unique constraint on (NoteId, UserId).
/// </summary>
/// <remarks>
/// DESIGN: The unique index is the ultimate guarantee of the
/// "one vote per user per note" invariant. Even if the application-level
/// check in VoteService races, this constraint prevents duplicates.
/// This is the "last line of defense" pattern common when splitting aggregates.
/// </remarks>
public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasKey(v => v.Id);
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(v => new { v.NoteId, v.UserId })
            .IsUnique()
            .HasDatabaseName("IX_Vote_NoteId_UserId");

        builder.HasOne<Note>()
            .WithMany()
            .HasForeignKey(v => v.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(v => v.DeletedAt == null);
    }
}
```

---

## 8. What This Tier Solves vs API 3

| Problem from API 3 | Solved? | How |
|---------------------|---------|-----|
| Aggregate explosion (huge RetroBoard) | ✅ | Votes no longer part of retro aggregate |
| Write contention on voting | ✅ | Each vote is its own aggregate — no locks on retro |
| Performance (loading all votes) | ✅ | Retro loads only columns + notes |

---

## 9. New Trade-offs Introduced

| Trade-off | Description |
|-----------|------------|
| **Cross-aggregate invariant enforcement** | "One vote per user per note" now relies on DB constraint + app-level check, not atomic aggregate method |
| **Eventual consistency** | A note could be deleted while a vote is being cast — FK constraint catches this but error handling is more complex |
| **More services** | VoteService reintroduced (was absorbed into RetroBoardService in API 3) |
| **Referential integrity complexity** | Soft-deleting a note should conceptually invalidate its votes — need to handle this (cascade or event) |

---

## 10. Files to Create / Modify vs API 3

| Action | Files |
|--------|-------|
| **New** | `VoteAggregate/Vote.cs`, `VoteAggregate/IVoteRepository.cs`, `VoteService.cs`, `VoteRepository.cs`, `VoteConfiguration.cs`, `VotesController.cs` |
| **Modify** | `RetroBoard.cs` (remove vote methods), `Note.cs` (remove votes collection), `RetroBoardRepository.cs` (remove votes include), `RetroBoardConfiguration.cs` |
| **Copy** | All other files largely identical to API 3 |
