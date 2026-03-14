using Api5.Domain.Common;
using Api5.Domain.VoteAggregate.Events;

namespace Api5.Domain.VoteAggregate;

/// <summary>
/// Aggregate root representing a single vote on a note by a user.
/// </summary>
/// <remarks>
/// DESIGN: Same aggregate boundaries as API 4 — Vote is its own aggregate
/// root with independent consistency and its own concurrency token.
///
/// New in API 5: Vote inherits from <see cref="AggregateRoot"/> and raises
/// a <see cref="VoteCastEvent"/> upon creation. This event enables decoupled
/// side effects (notifications, analytics) without the Vote aggregate or
/// its command handler knowing about those concerns.
///
/// The "one vote per user per note" invariant is enforced by:
///   1. An application-level check in <c>CastVoteCommandHandler</c>.
///   2. A DB unique constraint on (NoteId, UserId) as the safety net.
/// This is identical to API 4 — splitting the aggregate trade-off remains.
/// </remarks>
public class Vote : AggregateRoot
{
    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private Vote() { }

    /// <summary>
    /// Creates a new vote and raises a <see cref="VoteCastEvent"/>.
    /// </summary>
    /// <param name="noteId">The note being voted on.</param>
    /// <param name="userId">The user casting the vote.</param>
    public Vote(Guid noteId, Guid userId)
    {
        NoteId = noteId;
        UserId = userId;

        // DESIGN: Raise a domain event so handlers can react to voting activity.
        RaiseDomainEvent(new VoteCastEvent(Id, noteId, userId));
    }

    /// <summary>Gets the ID of the note this vote is for.</summary>
    public Guid NoteId { get; private set; }

    /// <summary>Gets the ID of the user who cast this vote.</summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the concurrency token mapped to PostgreSQL <c>xmin</c> system column.
    /// </summary>
    /// <remarks>
    /// DESIGN: Same as API 4. Vote has its own concurrency token because it is
    /// an aggregate root. Each vote row is independently versioned.
    /// </remarks>
    public uint Version { get; private set; }
}
