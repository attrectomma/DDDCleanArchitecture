using Api5.Application.DTOs.Requests;
using Api5.Application.DTOs.Responses;
using Api5.Application.Retros.Commands.AddColumn;
using Api5.Application.Retros.Commands.ChangeVotingStrategy;
using Api5.Application.Retros.Commands.CreateRetroBoard;
using Api5.Application.Retros.Commands.RemoveColumn;
using Api5.Application.Retros.Commands.RenameColumn;
using Api5.Application.Retros.Queries.GetRetroBoard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api5.WebApi.Controllers;

/// <summary>
/// Controller for retro board operations and column management.
/// Dispatches commands and queries via <see cref="IMediator"/>.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 4's RetroBoardsController that depended on
/// <c>IRetroBoardService</c>. Here, each action creates the appropriate
/// command or query and sends it through MediatR. The controller has no
/// knowledge of repositories, aggregates, or business logic.
/// </remarks>
[ApiController]
[Route("api")]
public class RetroBoardsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardsController"/>.
    /// </summary>
    /// <param name="mediator">The MediatR mediator.</param>
    public RetroBoardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── RetroBoard CRUD ─────────────────────────────────────────

    /// <summary>Creates a new retro board within a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="request">The retro board creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created retro board.</returns>
    [HttpPost("projects/{projectId:guid}/retros")]
    [ProducesResponseType(typeof(RetroBoardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid projectId, CreateRetroBoardRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRetroBoardCommand(
            projectId,
            request.Name,
            request.VotingStrategy ?? Domain.VoteAggregate.Strategies.VotingStrategyType.Default);
        RetroBoardResponse response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, retroId = response.Id }, response);
    }

    /// <summary>Retrieves a retro board by its ID, including all columns, notes, and vote counts.</summary>
    /// <param name="projectId">The project ID (for route context).</param>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board with details.</returns>
    /// <remarks>
    /// DESIGN (CQRS): This GET endpoint sends a <see cref="GetRetroBoardQuery"/>
    /// which is handled by a query handler that projects directly from the
    /// database with no-tracking. The handler does NOT load the RetroBoard
    /// aggregate — a key efficiency gain over API 3/4.
    /// </remarks>
    [HttpGet("projects/{projectId:guid}/retros/{retroId:guid}")]
    [ProducesResponseType(typeof(RetroBoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid projectId, Guid retroId, CancellationToken cancellationToken)
    {
        RetroBoardResponse response = await _mediator.Send(new GetRetroBoardQuery(retroId), cancellationToken);
        return Ok(response);
    }

    /// <summary>Changes the voting strategy for a retro board.</summary>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="request">The request containing the new voting strategy.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated retro board.</returns>
    /// <remarks>
    /// DESIGN: The Strategy pattern allows each board to independently choose
    /// its voting behaviour. Supported strategies:
    ///   - <b>Default</b>: One vote per user per note (API 1–4 behaviour).
    ///   - <b>Budget</b>: Dot voting — each user gets up to 3 votes per column
    ///     and may place multiple votes on the same note.
    ///
    /// Changing the strategy does not invalidate existing votes.
    /// </remarks>
    [HttpPut("retros/{retroId:guid}/voting-strategy")]
    [ProducesResponseType(typeof(RetroBoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeVotingStrategy(
        Guid retroId,
        ChangeVotingStrategyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeVotingStrategyCommand(retroId, request.VotingStrategy);
        RetroBoardResponse response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    // ── Column operations ───────────────────────────────────────

    /// <summary>Creates a new column in a retro board.</summary>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="request">The column creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created column.</returns>
    [HttpPost("retros/{retroId:guid}/columns")]
    [ProducesResponseType(typeof(ColumnResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddColumn(Guid retroId, CreateColumnRequest request, CancellationToken cancellationToken)
    {
        var command = new AddColumnCommand(retroId, request.Name);
        ColumnResponse response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(AddColumn), new { retroId, columnId = response.Id }, response);
    }

    /// <summary>Updates an existing column's name.</summary>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="request">The column update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated column.</returns>
    [HttpPut("retros/{retroId:guid}/columns/{columnId:guid}")]
    [ProducesResponseType(typeof(ColumnResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateColumn(Guid retroId, Guid columnId, UpdateColumnRequest request, CancellationToken cancellationToken)
    {
        var command = new RenameColumnCommand(retroId, columnId, request.Name);
        ColumnResponse response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    /// <summary>Soft-deletes a column.</summary>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("retros/{retroId:guid}/columns/{columnId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteColumn(Guid retroId, Guid columnId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveColumnCommand(retroId, columnId), cancellationToken);
        return NoContent();
    }
}
