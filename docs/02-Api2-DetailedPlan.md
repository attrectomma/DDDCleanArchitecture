# API 2 — Rich Domain Models (Detailed Implementation Plan)

> **Theme:** Same Clean Architecture layers as API 1, but **push all business logic
> possible into the domain entities**. Entities are no longer anemic — they
> enforce their own invariants. Services become thin orchestrators.

---

## 1. What Changes from API 1

| Aspect | API 1 | API 2 |
|--------|-------|-------|
| Entity design | Public setters, no methods | Private setters, factory methods, guard methods |
| Invariant enforcement | Service layer | Inside entity methods |
| Service responsibility | Validate + enforce rules + persist | Orchestrate (load → call entity method → save) |
| Repository interface | Same | Same (still per-table) |
| Controller | Same | Same |
| Concurrency | None | None (still no aggregate boundary) |

> **DESIGN:** API 2 demonstrates that you don't need DDD aggregates to have a
> rich domain. However, without aggregate boundaries, **concurrency and
> consistency remain unsolved** — invariants can still be violated under
> concurrent writes because there's no locking strategy.

---

## 2. Project Structure

Identical folder layout to API 1. The changes are *inside* the files.

```
src/Api2.RichDomain/
├── Api2.Domain/
│   ├── Entities/
│   │   ├── AuditableEntityBase.cs        (same as API 1)
│   │   ├── User.cs                       (+ factory method)
│   │   ├── Project.cs                    (+ AddMember, RemoveMember)
│   │   ├── ProjectMember.cs
│   │   ├── RetroBoard.cs                 (+ AddColumn)
│   │   ├── Column.cs                     (+ AddNote, Rename)
│   │   ├── Note.cs                       (+ CastVote, RemoveVote, UpdateText)
│   │   └── Vote.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs            (NEW — thrown by entities)
│   │   └── InvariantViolationException.cs
│   └── Api2.Domain.csproj
│
├── Api2.Application/                     (thinner services)
├── Api2.Infrastructure/                  (same as API 1)
└── Api2.WebApi/                          (same as API 1)
```

---

## 3. Rich Entity Design

### 3.1 Column (example)

```csharp
/// <summary>
/// Represents a retro column (e.g., "What went well", "Action items").
/// </summary>
/// <remarks>
/// DESIGN: Unlike API 1, the Column entity now owns the invariant
/// "note text must be unique within a column". The service no longer
/// does this check — it delegates to <see cref="AddNote"/>.
/// However, the Column must be loaded with its Notes collection
/// for the check to work, which couples loading strategy to domain logic.
/// API 3 resolves this by making Column part of the Retro aggregate.
/// </remarks>
public class Column : AuditableEntityBase
{
    private readonly List<Note> _notes = new();

    // EF Core needs this
    private Column() { }

    /// <summary>Creates a new column with the given name.</summary>
    public Column(Guid retroBoardId, string name)
    {
        RetroBoardId = retroBoardId;
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    public Guid RetroBoardId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<Note> Notes => _notes.AsReadOnly();

    /// <summary>
    /// Renames this column.
    /// </summary>
    /// <remarks>
    /// DESIGN: Uniqueness of the new name across sibling columns cannot
    /// be checked here because this entity doesn't know about its siblings.
    /// In API 2 the service still checks this. In API 3 the RetroBoard
    /// aggregate root owns the full column list and can enforce this.
    /// </remarks>
    public void Rename(string newName)
    {
        Name = Guard.AgainstNullOrWhiteSpace(newName, nameof(newName));
    }

    /// <summary>
    /// Adds a note to this column, enforcing the unique-text invariant.
    /// </summary>
    /// <exception cref="InvariantViolationException">
    /// Thrown when a note with the same text already exists in this column.
    /// </exception>
    public Note AddNote(string text)
    {
        if (_notes.Any(n => n.Text.Equals(text, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException($"A note with text '{text}' already exists in this column.");

        var note = new Note(Id, text);
        _notes.Add(note);
        return note;
    }
}
```

### 3.2 Note (vote invariant)

```csharp
/// <summary>
/// A sticky note on a retro column.
/// </summary>
public class Note : AuditableEntityBase
{
    private readonly List<Vote> _votes = new();

    private Note() { }

    public Note(Guid columnId, string text)
    {
        ColumnId = columnId;
        Text = Guard.AgainstNullOrWhiteSpace(text, nameof(text));
    }

    public Guid ColumnId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public IReadOnlyCollection<Vote> Votes => _votes.AsReadOnly();

    /// <summary>
    /// Casts a vote on behalf of a user.
    /// Enforces: a user may cast only 1 vote per note.
    /// </summary>
    public Vote CastVote(Guid userId)
    {
        if (_votes.Any(v => v.UserId == userId))
            throw new InvariantViolationException($"User {userId} has already voted on this note.");

        var vote = new Vote(Id, userId);
        _votes.Add(vote);
        return vote;
    }

    /// <summary>Removes a vote by its id.</summary>
    public void RemoveVote(Guid voteId)
    {
        var vote = _votes.FirstOrDefault(v => v.Id == voteId)
            ?? throw new DomainException($"Vote {voteId} not found on this note.");
        _votes.Remove(vote);
    }

    /// <summary>Updates the note text.</summary>
    public void UpdateText(string newText)
    {
        Text = Guard.AgainstNullOrWhiteSpace(newText, nameof(newText));
    }
}
```

### 3.3 Project (membership)

```csharp
/// <summary>
/// A project that groups users and retro boards together.
/// </summary>
public class Project : AuditableEntityBase
{
    private readonly List<ProjectMember> _members = new();

    private Project() { }

    public Project(string name)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    /// <summary>Assigns a user to this project.</summary>
    public ProjectMember AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvariantViolationException($"User {userId} is already a member of this project.");

        var member = new ProjectMember(Id, userId);
        _members.Add(member);
        return member;
    }

    /// <summary>Removes a user from this project.</summary>
    public void RemoveMember(Guid userId)
    {
        var member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException($"User {userId} is not a member of this project.");
        _members.Remove(member);
    }
}
```

---

## 4. Thin Services (Orchestration Only)

```csharp
/// <summary>
/// Orchestrates column operations by loading entities, invoking domain
/// behaviour, and persisting changes.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 1's ColumnService — the uniqueness check
/// for notes has moved into <see cref="Column.AddNote"/>. However, the
/// uniqueness check for *column names across the retro* still lives here
/// because a Column doesn't know about its siblings. API 3 solves this
/// by making RetroBoard the aggregate root that owns all columns.
/// </remarks>
public class ColumnService : IColumnService
{
    public async Task<ColumnResponse> CreateAsync(Guid retroBoardId, CreateColumnRequest request, CancellationToken ct)
    {
        var retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, ct)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // DESIGN: Cross-entity invariant — can't be inside Column itself.
        // Still a check-then-act race condition (same as API 1).
        if (await _columnRepository.ExistsByNameInRetroAsync(retroBoardId, request.Name, ct))
            throw new DuplicateException("Column", "Name", request.Name);

        var column = new Column(retroBoardId, request.Name); // factory via constructor
        await _columnRepository.AddAsync(column, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToResponse(column);
    }
}
```

```csharp
/// <summary>
/// Note service — now delegates vote invariant to the Note entity.
/// </summary>
public class NoteService : INoteService
{
    public async Task<VoteResponse> CastVoteAsync(Guid noteId, CastVoteRequest request, CancellationToken ct)
    {
        // DESIGN: Must load note WITH votes for the domain check to work.
        // This is a hidden coupling between loading strategy and domain logic.
        var note = await _noteRepository.GetByIdWithVotesAsync(noteId, ct)
            ?? throw new NotFoundException("Note", noteId);

        // Invariant enforced inside the entity
        var vote = note.CastVote(request.UserId);

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToResponse(vote);
    }
}
```

---

## 5. Repository Changes

Repositories need additional `Include`-based methods to load child collections:

```csharp
public interface INoteRepository : IRepository<Note>
{
    /// <summary>
    /// Loads a note with its Votes collection, needed for
    /// <see cref="Note.CastVote"/> invariant check.
    /// </summary>
    Task<Note?> GetByIdWithVotesAsync(Guid id, CancellationToken ct = default);
}

public interface IColumnRepository : IRepository<Column>
{
    Task<Column?> GetByIdWithNotesAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameInRetroAsync(Guid retroBoardId, string name, CancellationToken ct = default);
}
```

---

## 6. Domain Exceptions

```csharp
/// <summary>
/// Thrown when a domain invariant is violated (e.g., duplicate vote).
/// </summary>
/// <remarks>
/// DESIGN: In API 1 these were Application-layer exceptions.
/// Moving them to the Domain layer signals that the domain itself
/// is responsible for enforcing its rules.
/// </remarks>
public class InvariantViolationException : DomainException
{
    public InvariantViolationException(string message) : base(message) { }
}
```

---

## 7. Infrastructure & WebApi

- **Infrastructure**: Identical to API 1 except repository implementations need `Include()` calls for child collections.
- **EF Core configurations**: Updated to map private backing fields (`_notes`, `_votes`, `_members`).
- **WebApi/Controllers**: Identical to API 1.
- **Middleware**: Maps `DomainException` / `InvariantViolationException` → 409 Conflict or 422 Unprocessable Entity.

---

## 8. What's Still Broken (documented intentionally)

| Weakness | Still Present? | Notes |
|----------|---------------|-------|
| Race conditions on uniqueness | ✅ Yes | Column name uniqueness across retro is still check-then-act in service |
| No optimistic concurrency | ✅ Yes | No concurrency token |
| No aggregate boundary | ✅ Yes | Entities are loaded individually |
| Scattered cross-entity rules | ✅ Partially | Within-entity rules are centralized, but cross-entity rules still in services |
| Loading strategy coupling | 🆕 New issue | Must remember to `Include()` children or domain checks silently pass |

> **Key teaching point:** Rich domain models are better than anemic ones,
> but without aggregate boundaries the consistency model is still fragile.
> API 3 introduces aggregates to address this.

---

## 9. Files to Create / Modify vs API 1

| Action | Files |
|--------|-------|
| **New** | `DomainException.cs`, `InvariantViolationException.cs`, `Guard.cs` |
| **Rewrite** | All entity classes (private setters, constructors, methods) |
| **Simplify** | All service classes (remove invariant checks that moved to domain) |
| **Modify** | Repository interfaces & implementations (add `WithXxx` loading methods) |
| **Modify** | EF configurations (backing field access mode) |
| **Copy** | Controllers, middleware, Program.cs (minimal changes) |
