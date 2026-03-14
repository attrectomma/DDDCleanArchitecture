namespace Api2.Domain.Entities;

/// <summary>
/// Represents a vote cast by a <see cref="User"/> on a <see cref="Note"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 Vote was a plain DTO created directly by the
/// <c>VoteService</c>. In API 2, Vote instances are created by
/// <see cref="Note.CastVote"/> and removed by <see cref="Note.RemoveVote"/>.
/// The one-vote-per-user-per-note invariant is enforced by the Note entity
/// rather than the service layer.
///
/// A unique index on (NoteId, UserId) in the database still acts as a
/// safety net, but under API 2's architecture, concurrent writes can still
/// bypass the in-memory check because there is no aggregate-level
/// concurrency token. API 3+ add optimistic concurrency to close this gap.
/// </remarks>
public class Vote : AuditableEntityBase
{
    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private Vote() { }

    /// <summary>
    /// Creates a new vote.
    /// </summary>
    /// <param name="noteId">The ID of the note this vote is for.</param>
    /// <param name="userId">The ID of the user casting the vote.</param>
    public Vote(Guid noteId, Guid userId)
    {
        NoteId = noteId;
        UserId = userId;
    }

    /// <summary>Gets the ID of the note this vote is for.</summary>
    public Guid NoteId { get; private set; }

    /// <summary>Gets the ID of the user who cast this vote.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the navigation property to the note.</summary>
    public Note Note { get; private set; } = null!;

    /// <summary>Gets the navigation property to the user.</summary>
    public User User { get; private set; } = null!;
}
