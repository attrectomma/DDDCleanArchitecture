using Api3.Domain.Common;
using Api3.Domain.Exceptions;

namespace Api3.Domain.RetroAggregate;

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
///
/// DESIGN (CQRS foreshadowing): This same expensive query runs for
/// BOTH writes (where the full state is needed for invariants) AND
/// reads (where we only need a DTO). Loading the full aggregate graph
/// with change tracking just to map it to a response is wasteful.
/// API 5 introduces CQRS to separate the read path (lightweight
/// no-tracking projections) from the write path (full aggregate loading).
/// </remarks>
public class RetroBoard : AuditableEntityBase, IAggregateRoot
{
    private readonly List<Column> _columns = new();

    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private RetroBoard() { }

    /// <summary>
    /// Creates a new retro board for the specified project.
    /// </summary>
    /// <param name="projectId">The ID of the project this retro board belongs to.</param>
    /// <param name="name">The name of the retro board.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    public RetroBoard(Guid projectId, string name)
    {
        ProjectId = projectId;
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    /// <summary>Gets the ID of the project this retro board belongs to.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Gets the name of the retro board.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the concurrency token mapped to PostgreSQL <c>xmin</c> system column.
    /// </summary>
    /// <remarks>
    /// DESIGN: xmin is a PostgreSQL system column that changes on every
    /// row update. Using it as a concurrency token means that if two
    /// requests load the same retro and both UPDATE this row, the second
    /// SaveChanges will throw <c>DbUpdateConcurrencyException</c>.
    ///
    /// By itself, xmin is only checked when the root row is modified.
    /// Child INSERTs (e.g., adding a column) do not trigger an UPDATE.
    /// To close this gap, every mutating method calls <see cref="BumpVersion"/>,
    /// which touches <see cref="AuditableEntityBase.LastUpdatedAt"/> to force
    /// EF Core to generate an UPDATE on the root row. This is the standard
    /// DDD approach recommended by Vaughn Vernon: the aggregate root's version
    /// must advance on every mutation, not just root-property changes.
    /// </remarks>
    public uint Version { get; private set; }

    /// <summary>Gets the read-only collection of columns in this retro board.</summary>
    public IReadOnlyCollection<Column> Columns => _columns.AsReadOnly();

    // ── Version bumping ─────────────────────────────────────────────

    /// <summary>
    /// Forces EF Core to detect this aggregate root as Modified, which
    /// triggers the xmin concurrency check on <c>SaveChanges</c>.
    /// </summary>
    /// <remarks>
    /// DESIGN: Without this call, adding a child entity (Column, Note, Vote)
    /// only generates an INSERT on the child table — the root row is untouched,
    /// so xmin is never checked. By touching <see cref="AuditableEntityBase.LastUpdatedAt"/>,
    /// we force an UPDATE on the root row. Two concurrent mutations to the same
    /// aggregate now conflict via xmin, even if both are child INSERTs.
    ///
    /// This is the Vaughn Vernon pattern: every command that modifies the
    /// aggregate must advance its version. The trade-off is an extra UPDATE
    /// per write — acceptable for consistency, but it increases contention.
    /// API 4 mitigates this by extracting Vote into its own aggregate, so
    /// voting no longer bumps the RetroBoard’s version.
    ///
    /// See: Vaughn Vernon, "Implementing Domain-Driven Design" (2013),
    /// Chapter 10 — Aggregates, "Optimistic Concurrency".
    /// </remarks>
    private void BumpVersion()
    {
        LastUpdatedAt = DateTime.UtcNow;
    }

    // ── Column operations ───────────────────────────────────────

    /// <summary>
    /// Adds a column to this retro board.
    /// Enforces: column names must be unique within the board.
    /// </summary>
    /// <param name="name">The name of the column.</param>
    /// <returns>The created <see cref="Column"/> entity.</returns>
    /// <exception cref="InvariantViolationException">
    /// Thrown when a column with the same name already exists in this retro board.
    /// </exception>
    /// <remarks>
    /// DESIGN: In API 2, this check was in the service layer (via a repository
    /// query). Now it lives here in the aggregate root. Because the full
    /// aggregate is always loaded, the in-memory check is authoritative for
    /// non-concurrent requests. Under concurrency, <see cref="BumpVersion"/>
    /// forces an UPDATE on the root row so the second request’s SaveChanges
    /// detects the xmin mismatch and throws <c>DbUpdateConcurrencyException</c>.
    /// The DB unique constraint on (RetroBoardId, Name) remains as a defence-in-depth
    /// safety net.
    /// </remarks>
    public Column AddColumn(string name)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        if (_columns.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"Column name '{name}' already exists in retro '{Name}'.");

        var column = new Column(Id, name);
        _columns.Add(column);
        BumpVersion();
        return column;
    }

    /// <summary>
    /// Renames a column, enforcing uniqueness across sibling columns.
    /// </summary>
    /// <param name="columnId">The ID of the column to rename.</param>
    /// <param name="newName">The new name for the column.</param>
    /// <exception cref="DomainException">
    /// Thrown when the column is not found in this retro board.
    /// </exception>
    /// <exception cref="InvariantViolationException">
    /// Thrown when another column with the same name already exists.
    /// </exception>
    /// <remarks>
    /// DESIGN: In API 2, the service checked column name uniqueness via
    /// a repository query (<c>ExistsByNameInRetroAsync</c>). Now the
    /// aggregate root checks it directly because it owns all columns.
    /// </remarks>
    public void RenameColumn(Guid columnId, string newName)
    {
        Column column = GetColumnOrThrow(columnId);

        if (_columns.Any(c => c.Id != columnId &&
            c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"Column name '{newName}' already exists in retro '{Name}'.");

        column.Rename(newName);
        BumpVersion();
    }

    /// <summary>
    /// Removes a column from this retro board.
    /// </summary>
    /// <param name="columnId">The ID of the column to remove.</param>
    /// <exception cref="DomainException">
    /// Thrown when the column is not found in this retro board.
    /// </exception>
    public void RemoveColumn(Guid columnId)
    {
        Column column = GetColumnOrThrow(columnId);
        _columns.Remove(column);
        BumpVersion();
    }

    // ── Note operations ─────────────────────────────────────────

    /// <summary>
    /// Adds a note to a specific column. The column enforces note text uniqueness.
    /// </summary>
    /// <param name="columnId">The ID of the column to add the note to.</param>
    /// <param name="text">The text content of the note.</param>
    /// <returns>The created <see cref="Note"/> entity.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the column is not found in this retro board.
    /// </exception>
    /// <exception cref="InvariantViolationException">
    /// Thrown when a note with the same text already exists in the column.
    /// </exception>
    public Note AddNote(Guid columnId, string text)
    {
        Column column = GetColumnOrThrow(columnId);
        Note note = column.AddNote(text);
        BumpVersion();
        return note;
    }

    /// <summary>
    /// Updates a note's text in a specific column. The column enforces uniqueness.
    /// </summary>
    /// <param name="columnId">The ID of the column containing the note.</param>
    /// <param name="noteId">The ID of the note to update.</param>
    /// <param name="newText">The new text for the note.</param>
    /// <exception cref="DomainException">
    /// Thrown when the column or note is not found.
    /// </exception>
    /// <exception cref="InvariantViolationException">
    /// Thrown when another note with the same text already exists in the column.
    /// </exception>
    /// <remarks>
    /// DESIGN: In API 2, note text uniqueness for updates was checked by the
    /// service via a repository query. Now the Column entity checks it because
    /// the full aggregate is loaded and Column has access to all its notes.
    /// </remarks>
    public void UpdateNote(Guid columnId, Guid noteId, string newText)
    {
        Column column = GetColumnOrThrow(columnId);
        column.UpdateNote(noteId, newText);
        BumpVersion();
    }

    /// <summary>
    /// Removes a note from a specific column.
    /// </summary>
    /// <param name="columnId">The ID of the column containing the note.</param>
    /// <param name="noteId">The ID of the note to remove.</param>
    /// <exception cref="DomainException">
    /// Thrown when the column or note is not found.
    /// </exception>
    public void RemoveNote(Guid columnId, Guid noteId)
    {
        Column column = GetColumnOrThrow(columnId);
        column.RemoveNote(noteId);
        BumpVersion();
    }

    // ── Vote operations ─────────────────────────────────────────

    /// <summary>
    /// Casts a vote on a note.
    /// </summary>
    /// <param name="columnId">The ID of the column containing the note.</param>
    /// <param name="noteId">The ID of the note to vote on.</param>
    /// <param name="userId">The ID of the user casting the vote.</param>
    /// <returns>The created <see cref="Vote"/> entity.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the column or note is not found.
    /// </exception>
    /// <exception cref="InvariantViolationException">
    /// Thrown when the user has already voted on this note.
    /// </exception>
    /// <remarks>
    /// DESIGN: <see cref="BumpVersion"/> forces an UPDATE on the root row,
    /// meaning two users voting on DIFFERENT notes in the same retro
    /// will conflict via xmin. This is the "aggregate explosion" problem —
    /// every write contends on the root. API 4 extracts Vote as its own
    /// aggregate to avoid this.
    /// </remarks>
    public Vote CastVote(Guid columnId, Guid noteId, Guid userId)
    {
        Column column = GetColumnOrThrow(columnId);
        Note note = column.GetNoteOrThrow(noteId);
        Vote vote = note.CastVote(userId);
        BumpVersion();
        return vote;
    }

    /// <summary>
    /// Removes a vote from a note.
    /// </summary>
    /// <param name="columnId">The ID of the column containing the note.</param>
    /// <param name="noteId">The ID of the note containing the vote.</param>
    /// <param name="voteId">The ID of the vote to remove.</param>
    /// <exception cref="DomainException">
    /// Thrown when the column, note, or vote is not found.
    /// </exception>
    public void RemoveVote(Guid columnId, Guid noteId, Guid voteId)
    {
        Column column = GetColumnOrThrow(columnId);
        Note note = column.GetNoteOrThrow(noteId);
        note.RemoveVote(voteId);
        BumpVersion();
    }

    // ── Private helpers ─────────────────────────────────────────

    /// <summary>
    /// Finds a column by ID within this aggregate or throws.
    /// </summary>
    private Column GetColumnOrThrow(Guid columnId) =>
        _columns.FirstOrDefault(c => c.Id == columnId)
        ?? throw new DomainException($"Column {columnId} not found in retro {Id}.");
}
