using Api5.Domain.Common;

namespace Api5.Domain.VoteAggregate.Events;

/// <summary>
/// Raised when a vote is cast on a note.
/// </summary>
/// <remarks>
/// DESIGN: This event enables decoupled reactions to voting activity.
/// Handlers could update real-time vote count displays, send notifications,
/// or maintain analytics. The Vote aggregate raises this event in its
/// constructor, signalling that a vote was created. In API 4, any such
/// reactions would have been inlined in the VoteService.CastVoteAsync method.
/// </remarks>
/// <param name="VoteId">The ID of the newly created vote.</param>
/// <param name="NoteId">The ID of the note that was voted on.</param>
/// <param name="UserId">The ID of the user who cast the vote.</param>
public record VoteCastEvent(Guid VoteId, Guid NoteId, Guid UserId) : IDomainEvent
{
    /// <inheritdoc />
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
