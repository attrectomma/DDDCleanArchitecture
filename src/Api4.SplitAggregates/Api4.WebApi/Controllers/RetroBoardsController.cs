using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;
using Api4.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api4.WebApi.Controllers;

/// <summary>
/// Controller for retro board operations and column management.
/// </summary>
/// <remarks>
/// DESIGN: Similar to API 3 but without vote operations. Column operations
/// are still handled here because columns are part of the RetroBoard aggregate.
/// Vote operations moved to <see cref="VotesController"/>.
/// </remarks>
[ApiController]
[Route("api")]
public class RetroBoardsController : ControllerBase
{
    private readonly IRetroBoardService _retroBoardService;

    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardsController"/>.
    /// </summary>
    /// <param name="retroBoardService">The retro board service.</param>
    public RetroBoardsController(IRetroBoardService retroBoardService)
    {
        _retroBoardService = retroBoardService;
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
        RetroBoardResponse response = await _retroBoardService.CreateAsync(projectId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { projectId, retroId = response.Id }, response);
    }

    /// <summary>Retrieves a retro board by its ID, including all columns, notes, and vote counts.</summary>
    /// <param name="projectId">The project ID (for route context).</param>
    /// <param name="retroId">The retro board ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board with details.</returns>
    [HttpGet("projects/{projectId:guid}/retros/{retroId:guid}")]
    [ProducesResponseType(typeof(RetroBoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid projectId, Guid retroId, CancellationToken cancellationToken)
    {
        RetroBoardResponse response = await _retroBoardService.GetByIdAsync(retroId, cancellationToken);
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
        ColumnResponse response = await _retroBoardService.AddColumnAsync(retroId, request, cancellationToken);
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
        ColumnResponse response = await _retroBoardService.RenameColumnAsync(retroId, columnId, request, cancellationToken);
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
        await _retroBoardService.RemoveColumnAsync(retroId, columnId, cancellationToken);
        return NoContent();
    }
}
