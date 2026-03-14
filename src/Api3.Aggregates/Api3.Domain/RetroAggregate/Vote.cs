using Api3.Domain.Common;
using Api3.Domain.Exceptions;

namespace Api3.Domain.RetroAggregate;

/// <summary>
/// Represents a vote cast by a user on a <see cref="Note"/>.
/// Child entity within the RetroBoard aggregate.
/// </summary>
/// <remarks>
/// DESIGN: In API 1/2, Vote had its own repository. In API 3, Vote is a
/// child entity deep inside the RetroBoard aggregate: RetroBoard → Column →
/// Note → Vote. Votes are created by <see cref="Note.CastVote"/> and removed
/// by <see cref="Note.RemoveVote"/>. There is no <c>IVoteRepository</c>.
///
/// TRADE-OFF: Because Vote is inside the RetroBoard aggregate, casting a vote
/// requires loading the ENTIRE aggregate (all columns, notes, and votes) and
/// taking a write lock (xmin) on the aggregate root. Two users voting on
/// different notes in the same retro will conflict. API 4 extracts Vote as
/// its own aggregate to eliminate this contention.
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
}
