using Api2.Domain.Exceptions;

namespace Api2.Domain.Entities;

/// <summary>
/// Represents a retro column (e.g., "What went well", "What to improve").
/// A column belongs to a <see cref="RetroBoard"/> and contains <see cref="Note"/>s.
/// </summary>
/// <remarks>
/// DESIGN: Unlike API 1, the Column entity now owns the invariant
/// "note text must be unique within a column". The service no longer
/// does this check for creation — it delegates to <see cref="AddNote"/>.
/// However, the Column must be loaded with its <see cref="Notes"/> collection
/// for the check to work, which couples loading strategy to domain logic.
/// API 3 resolves this by making Column part of the RetroBoard aggregate.
///
/// Column name uniqueness across sibling columns cannot be checked here
/// because this entity doesn't know about its siblings. In API 2 the
/// service still checks this via a repository query. In API 3 the
/// RetroBoard aggregate root owns the full column list and can enforce this.
/// </remarks>
public class Column : AuditableEntityBase
{
    private readonly List<Note> _notes = new();

    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private Column() { }

    /// <summary>
    /// Creates a new column with the given name.
    /// </summary>
    /// <param name="retroBoardId">The ID of the retro board this column belongs to.</param>
    /// <param name="name">The name of the column.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    public Column(Guid retroBoardId, string name)
    {
        RetroBoardId = retroBoardId;
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    /// <summary>Gets the ID of the retro board this column belongs to.</summary>
    public Guid RetroBoardId { get; private set; }

    /// <summary>Gets the name of the column.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the navigation property to the owning retro board.</summary>
    public RetroBoard RetroBoard { get; private set; } = null!;

    /// <summary>Gets the read-only collection of notes in this column.</summary>
    public IReadOnlyCollection<Note> Notes => _notes.AsReadOnly();

    /// <summary>
    /// Renames this column.
    /// </summary>
    /// <param name="newName">The new name for the column.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newName"/> is null, empty, or whitespace.
    /// </exception>
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
    /// <param name="text">The text content of the note.</param>
    /// <returns>The created <see cref="Note"/> entity.</returns>
    /// <exception cref="InvariantViolationException">
    /// Thrown when a note with the same text already exists in this column.
    /// </exception>
    /// <remarks>
    /// DESIGN: In API 1, this uniqueness check lived in <c>NoteService</c>
    /// as a repository query. Now the Column entity enforces it directly.
    /// The Column must be loaded with its Notes collection for this to work.
    /// </remarks>
    public Note AddNote(string text)
    {
        if (_notes.Any(n => n.Text.Equals(text, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"A note with text '{text}' already exists in this column.");

        var note = new Note(Id, text);
        _notes.Add(note);
        return note;
    }
}
