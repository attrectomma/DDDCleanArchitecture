using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;

namespace Api5.Application.Votes.Commands.CastVote;

/// <summary>
/// Command to cast a vote on a note.
/// </summary>
/// <remarks>
/// DESIGN: Commands are simple immutable data objects. They describe
/// the user's INTENT, not the implementation. This is the key mindset
/// shift from noun-centric (VoteService.CastVote) to behavior-centric
/// (CastVoteCommand). The command name reads as a sentence:
/// "Cast a vote on note X by user Y."
///
/// Implements <see cref="ICommand{TResponse}"/> (not <c>IRequest</c>
/// directly) so the <c>TransactionBehavior</c> wraps this command's
/// execution — including any domain event handlers — in a single
/// atomic database transaction.
/// </remarks>
/// <param name="NoteId">The ID of the note to vote on.</param>
/// <param name="UserId">The ID of the user casting the vote.</param>
public record CastVoteCommand(Guid NoteId, Guid UserId) : ICommand<VoteResponse>;
