using Api5.Domain.Common;

namespace Api5.Domain.RetroAggregate;

/// <summary>
/// A sticky note within a column. Does NOT contain votes in API 5
/// (same as API 4 — votes are their own aggregate).
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 4. Note is a child entity within the RetroBoard
/// aggregate. It does not inherit from <see cref="AggregateRoot"/> because
/// it is not an aggregate root — it inherits from <see cref="AuditableEntityBase"/>
/// directly. Note does not raise domain events itself; the RetroBoard aggregate
/// root raises events on behalf of its children (e.g., <c>NoteRemovedEvent</c>
/// is raised by <see cref="RetroBoard.RemoveNote"/>).
/// </remarks>
public class Note : AuditableEntityBase
{
    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private Note() { }

    /// <summary>
    /// Creates a new note with the specified text.
    /// </summary>
    /// <param name="columnId">The ID of the column this note belongs to.</param>
    /// <param name="text">The text content of the note.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="text"/> is null, empty, or whitespace.
    /// </exception>
    public Note(Guid columnId, string text)
    {
        ColumnId = columnId;
        Text = Guard.AgainstNullOrWhiteSpace(text, nameof(text));
    }

    /// <summary>Gets the ID of the column this note belongs to.</summary>
    public Guid ColumnId { get; private set; }

    /// <summary>Gets the text content of the note.</summary>
    public string Text { get; private set; } = string.Empty;

    // ❌ No Votes collection — votes are a separate aggregate (same as API 4)

    /// <summary>
    /// Updates the text content of this note.
    /// </summary>
    /// <param name="newText">The new text for the note.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newText"/> is null, empty, or whitespace.
    /// </exception>
    public void UpdateText(string newText)
    {
        Text = Guard.AgainstNullOrWhiteSpace(newText, nameof(newText));
    }
}
