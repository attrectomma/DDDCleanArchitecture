using Api5.Domain.Common;
using Api5.Domain.Exceptions;
using Api5.Domain.RetroAggregate.Events;
using Api5.Domain.VoteAggregate.Strategies;

namespace Api5.Domain.RetroAggregate;

/// <summary>
/// Aggregate root for a retrospective board. Owns columns and notes,
/// but NOT votes (votes are their own aggregate, same as API 4).
/// </summary>
/// <remarks>
/// DESIGN: Same aggregate boundaries as API 4. The RetroBoard aggregate
/// owns Column and Note child entities and enforces invariants (unique
/// column names, unique note text within a column).
///
/// New in API 5: RetroBoard inherits from <see cref="AggregateRoot"/>
/// and raises domain events when structural changes occur:
///   - <see cref="ColumnAddedEvent"/> when a column is added.
///   - <see cref="NoteAddedEvent"/> when a note is added.
///   - <see cref="NoteRemovedEvent"/> when a note is removed.
///
/// The <c>NoteRemovedEvent</c> is particularly important — it enables
/// the <c>NoteRemovedEventHandler</c> in the Application layer to clean
/// up orphaned votes without the RetroBoard aggregate knowing about
/// the Vote aggregate. This decoupling was not possible in API 4.
///
/// CQRS: This aggregate is loaded ONLY for write operations (commands).
/// Read operations (queries) bypass the aggregate entirely and project
/// directly from the database — see <c>GetRetroBoardQueryHandler</c>.
/// </remarks>
public class RetroBoard : AggregateRoot
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
    /// <param name="votingStrategyType">
    /// The voting strategy for this board. Defaults to <see cref="VotingStrategyType.Default"/>
    /// (one vote per user per note).
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    public RetroBoard(Guid projectId, string name, VotingStrategyType votingStrategyType = VotingStrategyType.Default)
    {
        ProjectId = projectId;
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        VotingStrategyType = votingStrategyType;
    }

    /// <summary>Gets the ID of the project this retro board belongs to.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Gets the name of the retro board.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the voting strategy type for this retro board.
    /// Determines how votes are validated when users cast votes on notes.
    /// </summary>
    /// <remarks>
    /// DESIGN: The Strategy pattern allows each board to independently choose
    /// its voting behaviour. The <see cref="VotingStrategyType"/> is mapped to
    /// a concrete <see cref="IVotingStrategy"/> implementation by
    /// <see cref="VotingStrategyFactory"/> at vote-time. This keeps the
    /// RetroBoard aggregate decoupled from voting logic — it stores the
    /// configuration, not the behaviour.
    /// </remarks>
    public VotingStrategyType VotingStrategyType { get; private set; } = VotingStrategyType.Default;

    /// <summary>
    /// Gets the concurrency token mapped to PostgreSQL <c>xmin</c> system column.
    /// </summary>
    /// <remarks>
    /// DESIGN: Same as API 3/4. The xmin concurrency token protects the
    /// RetroBoard aggregate. Voting does NOT bump this token because Vote
    /// is its own aggregate. The xmin is only checked when the root row
    /// itself is UPDATEd. For concurrent child creation, the DB unique
    /// constraint is the actual safety net.
    /// </remarks>
    public uint Version { get; private set; }

    /// <summary>Gets the read-only collection of columns in this retro board.</summary>
    public IReadOnlyCollection<Column> Columns => _columns.AsReadOnly();

    // ── Voting strategy ────────────────────────────────────────

    /// <summary>
    /// Changes the voting strategy for this retro board.
    /// </summary>
    /// <param name="strategyType">The new voting strategy type.</param>
    /// <remarks>
    /// DESIGN: Changing the strategy is a configuration change on the aggregate.
    /// Existing votes are not affected — only future vote attempts will be
    /// validated against the new strategy. This is a conscious design choice:
    /// re-validating existing votes on strategy change would require loading
    /// all votes (a cross-aggregate operation) and could invalidate votes
    /// that were legitimate under the previous strategy.
    /// </remarks>
    public void SetVotingStrategy(VotingStrategyType strategyType)
    {
        VotingStrategyType = strategyType;
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
    public Column AddColumn(string name)
    {
        Guard.AgainstNullOrWhiteSpace(name, nameof(name));

        if (_columns.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"Column name '{name}' already exists in retro '{Name}'.");

        var column = new Column(Id, name);
        _columns.Add(column);

        // DESIGN: Raise a domain event to signal structural change.
        RaiseDomainEvent(new ColumnAddedEvent(Id, column.Id, column.Name));

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
        Note note = column.AddNote(text);

        // DESIGN: Raise a domain event to signal new content.
        RaiseDomainEvent(new NoteAddedEvent(Id, columnId, note.Id));

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
    /// <remarks>
    /// DESIGN: Raises <see cref="NoteRemovedEvent"/> so handlers can clean up
    /// orphaned votes. In API 4, this cleanup depended on either DB cascade
    /// delete or explicit VoteService logic. Domain events make it explicit
    /// and decoupled.
    /// </remarks>
    public void RemoveNote(Guid columnId, Guid noteId)
    {
        Column column = GetColumnOrThrow(columnId);
        column.RemoveNote(noteId);

        // DESIGN: This event triggers vote cleanup in NoteRemovedEventHandler.
        RaiseDomainEvent(new NoteRemovedEvent(noteId, columnId));
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
