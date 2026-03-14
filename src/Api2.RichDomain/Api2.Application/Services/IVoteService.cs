using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;

namespace Api2.Application.Services;

/// <summary>
/// Service contract for vote-related operations.
/// </summary>
public interface IVoteService
{
    /// <summary>Casts a vote on a note.</summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="request">The vote request containing the user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created vote response.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the note or user is not found.</exception>
    /// <exception cref="Api2.Domain.Exceptions.InvariantViolationException">
    /// Thrown when the user has already voted on this note.
    /// </exception>
    Task<VoteResponse> CastVoteAsync(Guid noteId, CastVoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a vote.</summary>
    /// <param name="noteId">The note ID (for route context).</param>
    /// <param name="voteId">The vote ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DeleteVoteAsync(Guid noteId, Guid voteId, CancellationToken cancellationToken = default);
}
