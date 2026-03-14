using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;
using Api2.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api2.WebApi.Controllers;

/// <summary>
/// Controller for project management and membership operations.
/// </summary>
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IProjectMemberService _projectMemberService;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectsController"/>.
    /// </summary>
    /// <param name="projectService">The project service.</param>
    /// <param name="projectMemberService">The project member service.</param>
    public ProjectsController(IProjectService projectService, IProjectMemberService projectMemberService)
    {
        _projectService = projectService;
        _projectMemberService = projectMemberService;
    }

    /// <summary>Creates a new project.</summary>
    /// <param name="request">The project creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created project.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        ProjectResponse response = await _projectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Retrieves a project by its ID.</summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The project.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        ProjectResponse response = await _projectService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    /// <summary>Adds a user as a member to a project.</summary>
    /// <param name="id">The project ID.</param>
    /// <param name="request">The add member request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created membership.</returns>
    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(typeof(ProjectMemberResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddMember(Guid id, AddMemberRequest request, CancellationToken cancellationToken)
    {
        ProjectMemberResponse response = await _projectMemberService.AddMemberAsync(id, request, cancellationToken);
        return Created($"/api/projects/{id}/members/{response.UserId}", response);
    }

    /// <summary>Removes a user from a project.</summary>
    /// <param name="id">The project ID.</param>
    /// <param name="userId">The user ID to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on success.</returns>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await _projectMemberService.RemoveMemberAsync(id, userId, cancellationToken);
        return NoContent();
    }
}
