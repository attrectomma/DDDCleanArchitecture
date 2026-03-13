using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;
using Api1.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api1.WebApi.Controllers;

/// <summary>
/// Controller for retro board operations.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/retros")]
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

    /// <summary>Creates a new retro board within a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="request">The retro board creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created retro board.</returns>
    [HttpPost]
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
    [HttpGet("{retroId:guid}")]
    [ProducesResponseType(typeof(RetroBoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid projectId, Guid retroId, CancellationToken cancellationToken)
    {
        RetroBoardResponse response = await _retroBoardService.GetByIdAsync(retroId, cancellationToken);
        return Ok(response);
    }
}
