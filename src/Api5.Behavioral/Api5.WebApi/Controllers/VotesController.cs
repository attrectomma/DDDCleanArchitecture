using Api5.Application.DTOs.Requests;
using Api5.Application.DTOs.Responses;
using Api5.Application.Votes.Commands.CastVote;
using Api5.Application.Votes.Commands.RemoveVote;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api5.WebApi.Controllers;

/// <summary>
/// Controller for vote operations on notes.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 4's VotesController that depended on
/// <c>IVoteService</c>. Here, each action creates the appropriate command
/// and sends it through MediatR. The controller knows nothing about the
/// Vote aggregate, repositories, or cross-aggregate validation logic.
///
/// API 4 introduced Vote as its own aggregate (split from RetroBoard).
/// API 5 keeps that split but replaces the service orchestration with
/// command handlers. The <c>CastVoteCommandHandler</c> coordinates the
/// same cross-aggregate checks (note existence, user membership, duplicate
/// detection) that <c>VoteService</c> did in API 4, but the controller
/// doesn't need to know which service or handler does the work.
/// </remarks>
[ApiController]
[Route("api/notes/{noteId:guid}/votes")]
public class VotesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of <see cref="VotesController"/>.
    /// </summary>
    /// <param name="mediator">The MediatR mediator.</param>
    public VotesController(IMediator mediator)
    {
        _mediator = mediator;
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
        var command = new CastVoteCommand(noteId, request.UserId);
        VoteResponse response = await _mediator.Send(command, cancellationToken);
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
        await _mediator.Send(new RemoveVoteCommand(noteId, voteId), cancellationToken);
        return NoContent();
    }
}
