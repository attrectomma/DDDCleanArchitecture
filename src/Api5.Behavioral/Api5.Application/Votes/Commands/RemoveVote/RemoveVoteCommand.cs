using MediatR;

namespace Api5.Application.Votes.Commands.RemoveVote;

/// <summary>
/// Command to remove a vote.
/// </summary>
/// <param name="NoteId">The ID of the note (for route context validation).</param>
/// <param name="VoteId">The ID of the vote to remove.</param>
public record RemoveVoteCommand(Guid NoteId, Guid VoteId) : IRequest<Unit>;
