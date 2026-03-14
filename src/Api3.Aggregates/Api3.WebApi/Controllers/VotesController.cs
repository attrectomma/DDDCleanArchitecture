using Api3.Application.DTOs.Requests;
using Api3.Application.DTOs.Responses;
using Api3.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api3.WebApi.Controllers;

/// <summary>
/// Controller for vote operations on notes.
/// </summary>
/// <remarks>
/// DESIGN: In API 3, votes are part of the RetroBoard aggregate. However,
/// the external REST contract uses <c>/api/notes/{noteId}/votes</c> which
/// does not include the retro board or column ID. The service internally
/// looks up the aggregate by note ID using
/// <see cref="IRetroBoardService.CastVoteByNoteIdAsync"/>.
///
/// This means casting a vote requires loading the ENTIRE retro board aggregate
/// just to add one vote — a deliberate trade-off for consistency. API 4
/// extracts Vote as its own aggregate to avoid this overhead.
/// </remarks>
[ApiController]
[Route("api/notes/{noteId:guid}/votes")]
public class VotesController : ControllerBase
{
    private readonly IRetroBoardService _retroBoardService;

    /// <summary>
    /// Initializes a new instance of <see cref="VotesController"/>.
    /// </summary>
    /// <param name="retroBoardService">The retro board service.</param>
    public VotesController(IRetroBoardService retroBoardService)
    {
        _retroBoardService = retroBoardService;
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
        VoteResponse response = await _retroBoardService.CastVoteByNoteIdAsync(noteId, request, cancellationToken);
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
        await _retroBoardService.RemoveVoteByNoteIdAsync(noteId, voteId, cancellationToken);
        return NoContent();
    }
}
