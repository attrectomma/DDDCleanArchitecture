using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;
using Api4.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api4.WebApi.Controllers;

/// <summary>
/// Controller for vote operations on notes.
/// </summary>
/// <remarks>
/// DESIGN: In API 3, this controller used <see cref="IRetroBoardService"/>
/// because votes were part of the RetroBoard aggregate. In API 4, Vote is
/// its own aggregate, so this controller uses <see cref="IVoteService"/>
/// for all vote operations. The VoteService coordinates across aggregates
/// (checking note existence, user membership, duplicate votes) before
/// creating the Vote aggregate.
///
/// This is a visible change at the controller level — the dependency changed
/// from IRetroBoardService to IVoteService. The URL contract remains the same.
/// </remarks>
[ApiController]
[Route("api/notes/{noteId:guid}/votes")]
public class VotesController : ControllerBase
{
    private readonly IVoteService _voteService;

    /// <summary>
    /// Initializes a new instance of <see cref="VotesController"/>.
    /// </summary>
    /// <param name="voteService">The vote service.</param>
    public VotesController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    /// <summary>Casts a vote on a note.</summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="request">The vote request containing the user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created vote.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(VoteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CastVote(Guid noteId, CastVoteRequest request, CancellationToken cancellationToken)
    {
        VoteResponse response = await _voteService.CastVoteAsync(noteId, request, cancellationToken);
        return Created($"/api/notes/{noteId}/votes/{response.Id}", response);
    }

    /// <summary>Removes a vote.</summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="voteId">The vote ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{voteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVote(Guid noteId, Guid voteId, CancellationToken cancellationToken)
    {
        await _voteService.RemoveVoteAsync(noteId, voteId, cancellationToken);
        return NoContent();
    }
}
