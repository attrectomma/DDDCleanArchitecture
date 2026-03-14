using Api2.Domain.Exceptions;

namespace Api2.Domain.Entities;

/// <summary>
/// A sticky note on a retro column. Contains text and can receive
/// <see cref="Vote"/>s from users.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 the Note entity was a plain DTO with no behaviour.
/// In API 2 it enforces the "one vote per user per note" invariant via
/// <see cref="CastVote"/> and provides <see cref="RemoveVote"/> and
/// <see cref="UpdateText"/> domain methods.
///
/// Note text uniqueness within a column is enforced at creation time by
/// <see cref="Column.AddNote"/>, not here — Note doesn't know about
/// sibling notes. For updates, the service still checks uniqueness
/// via a repository query because Note.UpdateText has no access to
/// sibling information.
/// </remarks>
public class Note : AuditableEntityBase
{
    private readonly List<Vote> _votes = new();

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

    /// <summary>Gets the navigation property to the owning column.</summary>
    public Column Column { get; private set; } = null!;

    /// <summary>Gets the read-only collection of votes on this note.</summary>
    public IReadOnlyCollection<Vote> Votes => _votes.AsReadOnly();

    /// <summary>
    /// Casts a vote on behalf of a user, enforcing the one-vote-per-user invariant.
    /// </summary>
    /// <param name="userId">The ID of the user casting the vote.</param>
    /// <returns>The created <see cref="Vote"/> entity.</returns>
    /// <exception cref="InvariantViolationException">
    /// Thrown when the user has already voted on this note.
    /// </exception>
    /// <remarks>
    /// DESIGN: In API 1, this invariant was checked by <c>VoteService</c>
    /// via a repository query (<c>ExistsAsync</c>). Now the Note entity
    /// enforces it directly. The Note must be loaded with its Votes
    /// collection for this check to work — a hidden coupling between
    /// loading strategy and domain logic that API 3+ resolve by always
    /// loading aggregate roots with their full child graph.
    /// </remarks>
    public Vote CastVote(Guid userId)
    {
        if (_votes.Any(v => v.UserId == userId))
            throw new InvariantViolationException(
                $"User {userId} has already voted on this note.");

        var vote = new Vote(Id, userId);
        _votes.Add(vote);
        return vote;
    }

    /// <summary>
    /// Removes a vote by its unique identifier.
    /// </summary>
    /// <param name="voteId">The ID of the vote to remove.</param>
    /// <exception cref="DomainException">
    /// Thrown when the vote is not found on this note.
    /// </exception>
    public void RemoveVote(Guid voteId)
    {
        Vote vote = _votes.FirstOrDefault(v => v.Id == voteId)
            ?? throw new DomainException($"Vote {voteId} not found on this note.");

        _votes.Remove(vote);
    }

    /// <summary>
    /// Updates the text content of this note.
    /// </summary>
    /// <param name="newText">The new text for the note.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newText"/> is null, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// DESIGN: Text uniqueness within the column cannot be checked here
    /// because this entity has no access to sibling notes. The service
    /// still performs this check via a repository query — same as API 1.
    /// API 3 resolves this by loading the entire RetroBoard aggregate.
    /// </remarks>
    public void UpdateText(string newText)
    {
        Text = Guard.AgainstNullOrWhiteSpace(newText, nameof(newText));
    }
}
