using Api4.Domain.Common;

namespace Api4.Domain.RetroAggregate;

/// <summary>
/// A sticky note within a column. Does NOT contain votes in API 4.
/// </summary>
/// <remarks>
/// DESIGN: In API 3, Note had a Votes collection and enforced the
/// one-vote-per-user invariant. In API 4, Vote is its own aggregate,
/// so Note is unaware of votes entirely. The vote uniqueness constraint
/// is enforced by the Vote aggregate + a DB unique index.
///
/// This simplification means:
///   - Note no longer has a <c>CastVote</c> or <c>RemoveVote</c> method.
///   - Note no longer has a <c>Votes</c> collection.
///   - Loading a Note is cheaper because it doesn't pull in votes.
///   - Vote count must be queried separately when needed for display.
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

    // ❌ No Votes collection — votes are a separate aggregate in API 4

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
