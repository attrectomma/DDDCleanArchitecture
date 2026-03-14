using Api4.Domain.Common;
using Api4.Domain.Exceptions;

namespace Api4.Domain.RetroAggregate;

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
///
/// DESIGN (CQRS foreshadowing): Even though we split Vote out (reducing
/// load size), we still load the full RetroBoard aggregate for EVERY
/// operation — including GET requests that only need a read-only view.
/// API 5's CQRS pattern addresses this: queries bypass the aggregate
/// entirely and project directly from the database with no-tracking.
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
    /// DESIGN: Same as API 3. The xmin concurrency token still protects the
    /// RetroBoard aggregate — but now voting does NOT bump this token because
    /// Vote is its own aggregate. Only column and note operations conflict.
    /// </remarks>
    public uint Version { get; private set; }

    /// <summary>Gets the read-only collection of columns in this retro board.</summary>
    public IReadOnlyCollection<Column> Columns => _columns.AsReadOnly();

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
    public void RenameColumn(Guid columnId, string newName)
    {
        Column column = GetColumnOrThrow(columnId);

        if (_columns.Any(c => c.Id != columnId &&
            c.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"Column name '{newName}' already exists in retro '{Name}'.");

        column.Rename(newName);
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
        return column.AddNote(text);
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
    public void UpdateNote(Guid columnId, Guid noteId, string newText)
    {
        Column column = GetColumnOrThrow(columnId);
        column.UpdateNote(noteId, newText);
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
    }

    // ── ❌ NO vote operations — votes are a separate aggregate ──

    // ── Private helpers ─────────────────────────────────────────

    /// <summary>
    /// Finds a column by ID within this aggregate or throws.
    /// </summary>
    private Column GetColumnOrThrow(Guid columnId) =>
        _columns.FirstOrDefault(c => c.Id == columnId)
        ?? throw new DomainException($"Column {columnId} not found in retro {Id}.");
}
