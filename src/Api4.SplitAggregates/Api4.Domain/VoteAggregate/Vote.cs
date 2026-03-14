using Api4.Domain.Common;

namespace Api4.Domain.VoteAggregate;

/// <summary>
/// Aggregate root representing a single vote on a note by a user.
/// </summary>
/// <remarks>
/// DESIGN: Extracting Vote as its own aggregate means:
///   - Each vote is an independent unit of consistency.
///   - Voting on note A doesn't conflict with voting on note B.
///   - The "one vote per user per note" invariant is enforced by:
///     1. A unique index on (NoteId, UserId) in the database.
///     2. An application-level check in VoteService before creation.
///   - This is an <b>eventual consistency</b> trade-off: the app-level check
///     can race, but the DB constraint provides the ultimate safety net.
///
/// TRADE-OFF: If the DB constraint catches a duplicate, we must handle
/// the DbUpdateException and return a meaningful error, not a 500.
///
/// Compare with API 3 where Vote was a child entity deep inside the
/// RetroBoard aggregate (RetroBoard → Column → Note → Vote). In API 3,
/// casting a vote required loading the ENTIRE retro board aggregate and
/// taking a write lock on its xmin. Now, voting is a lightweight,
/// independent operation.
/// </remarks>
public class Vote : AuditableEntityBase, IAggregateRoot
{
    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private Vote() { }

    /// <summary>
    /// Creates a new vote.
    /// </summary>
    /// <param name="noteId">The note being voted on.</param>
    /// <param name="userId">The user casting the vote.</param>
    public Vote(Guid noteId, Guid userId)
    {
        NoteId = noteId;
        UserId = userId;
    }

    /// <summary>Gets the ID of the note this vote is for.</summary>
    public Guid NoteId { get; private set; }

    /// <summary>Gets the ID of the user who cast this vote.</summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Gets the concurrency token mapped to PostgreSQL <c>xmin</c> system column.
    /// </summary>
    /// <remarks>
    /// DESIGN: Vote now has its own concurrency token because it is an aggregate
    /// root. In API 3, Vote had no version — concurrency was managed by the
    /// RetroBoard aggregate root's xmin. Now each vote row is independently
    /// versioned, which is fine because votes are typically immutable
    /// (create or delete, never update).
    /// </remarks>
    public uint Version { get; private set; }
}
