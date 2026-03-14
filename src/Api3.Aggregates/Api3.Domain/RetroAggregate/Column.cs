using Api3.Domain.Common;
using Api3.Domain.Exceptions;

namespace Api3.Domain.RetroAggregate;

/// <summary>
/// Represents a retro column (e.g., "What went well", "What to improve").
/// A column belongs to a <see cref="RetroBoard"/> and contains <see cref="Note"/>s.
/// Child entity within the RetroBoard aggregate.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, Column had its own repository and controller. In API 3,
/// Column is a child entity within the RetroBoard aggregate. It is only
/// reachable through the aggregate root. Column name uniqueness across
/// siblings is enforced by the RetroBoard aggregate root's <c>AddColumn</c>
/// and <c>RenameColumn</c> methods. Note text uniqueness within a column
/// is enforced here because Column has access to its notes collection.
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
    /// DESIGN: In API 2, uniqueness of the new name across sibling columns
    /// was checked by the service layer. In API 3, the RetroBoard aggregate
    /// root calls this method only after verifying uniqueness across all
    /// columns in its <see cref="RetroBoard.RenameColumn"/> method.
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
    /// DESIGN: Same as API 2. Column can enforce note text uniqueness because
    /// it owns its Notes collection. In API 3 this is always safe because
    /// the aggregate is always loaded with the full graph.
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

    /// <summary>
    /// Updates a note's text within this column, enforcing uniqueness.
    /// </summary>
    /// <param name="noteId">The ID of the note to update.</param>
    /// <param name="newText">The new text for the note.</param>
    /// <exception cref="DomainException">
    /// Thrown when the note is not found in this column.
    /// </exception>
    /// <exception cref="InvariantViolationException">
    /// Thrown when another note with the same text already exists in this column.
    /// </exception>
    /// <remarks>
    /// DESIGN: In API 2, text uniqueness for updates was checked by the service
    /// via a repository query because Note.UpdateText had no access to siblings.
    /// In API 3, Column can check sibling uniqueness directly because the full
    /// aggregate is loaded. This eliminates the check-then-act race condition.
    /// </remarks>
    public void UpdateNote(Guid noteId, string newText)
    {
        Note note = GetNoteOrThrow(noteId);

        if (_notes.Any(n => n.Id != noteId &&
            n.Text.Equals(newText, StringComparison.OrdinalIgnoreCase)))
            throw new InvariantViolationException(
                $"A note with text '{newText}' already exists in this column.");

        note.UpdateText(newText);
    }

    /// <summary>
    /// Removes a note from this column.
    /// </summary>
    /// <param name="noteId">The ID of the note to remove.</param>
    /// <exception cref="DomainException">
    /// Thrown when the note is not found in this column.
    /// </exception>
    public void RemoveNote(Guid noteId)
    {
        Note note = GetNoteOrThrow(noteId);
        _notes.Remove(note);
    }

    /// <summary>
    /// Retrieves a note by ID or throws a domain exception.
    /// </summary>
    /// <param name="noteId">The note ID to find.</param>
    /// <returns>The note entity.</returns>
    /// <exception cref="DomainException">
    /// Thrown when the note is not found in this column.
    /// </exception>
    public Note GetNoteOrThrow(Guid noteId) =>
        _notes.FirstOrDefault(n => n.Id == noteId)
        ?? throw new DomainException($"Note {noteId} not found in column {Id}.");
}
