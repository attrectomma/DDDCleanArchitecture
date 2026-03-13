using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;
using Api1.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api1.WebApi.Controllers;

/// <summary>
/// Controller for vote operations on notes.
/// </summary>
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
        await _voteService.DeleteVoteAsync(noteId, voteId, cancellationToken);
        return NoContent();
    }
}
