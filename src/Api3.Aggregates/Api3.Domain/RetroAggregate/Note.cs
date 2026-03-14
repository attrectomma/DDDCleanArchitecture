using Api3.Domain.Common;
using Api3.Domain.Exceptions;

namespace Api3.Domain.RetroAggregate;

/// <summary>
/// A sticky note on a retro column. Contains text and can receive
/// <see cref="Vote"/>s from users. Child entity within the RetroBoard aggregate.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, Note had its own repository and the service layer
/// was responsible for some cross-entity checks. In API 3, Note is a child
/// entity within the RetroBoard aggregate. It is only reachable through
/// the aggregate root via Column. The Note enforces the "one vote per user
/// per note" invariant and provides text update capability.
///
/// Note text uniqueness within a column is enforced by <see cref="Column.AddNote"/>
/// and <see cref="Column.UpdateNote"/>. The Note itself cannot check sibling
/// uniqueness, but the Column (its parent within the aggregate) can.
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
    /// DESIGN: Same check as API 2, but now protected by the aggregate's
    /// concurrency token. The full aggregate is loaded, so the in-memory
    /// check is authoritative. Concurrent writes to the same aggregate
    /// will be caught by the xmin concurrency token.
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
    /// DESIGN: In API 2, text uniqueness for updates could not be checked
    /// inside Note because it had no access to sibling notes. In API 3,
    /// text uniqueness for updates is checked by the Column entity (which
    /// is called by the RetroBoard aggregate root's <c>UpdateNote</c> method).
    /// </remarks>
    public void UpdateText(string newText)
    {
        Text = Guard.AgainstNullOrWhiteSpace(newText, nameof(newText));
    }
}
