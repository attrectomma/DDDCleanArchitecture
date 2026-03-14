using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;

namespace Api4.Application.Services;

/// <summary>
/// Service contract for vote operations. Handles cross-aggregate
/// coordination between the Vote, RetroBoard, and Project aggregates.
/// </summary>
/// <remarks>
/// DESIGN: This service is NEW in API 4. In API 3, vote operations were
/// handled by <see cref="IRetroBoardService"/> because Vote was part of
/// the RetroBoard aggregate. Now that Vote is its own aggregate root,
/// it needs its own service to coordinate cross-aggregate checks:
///   1. Does the note exist? (read from RetroBoard aggregate boundary)
///   2. Has the user already voted? (read from Vote aggregate)
///   3. Create and persist the Vote aggregate.
/// </remarks>
public interface IVoteService
{
    /// <summary>Casts a vote on a note.</summary>
    /// <param name="noteId">The ID of the note to vote on.</param>
    /// <param name="request">The vote request containing the user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created vote response.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the note is not found.</exception>
    /// <exception cref="Api4.Domain.Exceptions.InvariantViolationException">
    /// Thrown when the user has already voted on this note.
    /// </exception>
    Task<VoteResponse> CastVoteAsync(Guid noteId, CastVoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a vote.</summary>
    /// <param name="noteId">The note ID (for route context).</param>
    /// <param name="voteId">The vote ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the vote is not found.</exception>
    Task RemoveVoteAsync(Guid noteId, Guid voteId, CancellationToken cancellationToken = default);
}
