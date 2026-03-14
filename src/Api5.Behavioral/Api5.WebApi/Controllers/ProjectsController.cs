using Api5.Application.DTOs.Requests;
using Api5.Application.DTOs.Responses;
using Api5.Application.Projects.Commands.AddMember;
using Api5.Application.Projects.Commands.CreateProject;
using Api5.Application.Projects.Commands.RemoveMember;
using Api5.Application.Projects.Queries.GetProject;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api5.WebApi.Controllers;

/// <summary>
/// Controller for project management and membership operations.
/// Dispatches commands and queries via <see cref="IMediator"/>.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 4's ProjectsController that depended on
/// <c>IProjectService</c>. Here, the controller only knows
/// <see cref="IMediator"/>. Each action maps the HTTP request to a
/// command or query and sends it through the MediatR pipeline.
/// </remarks>
[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectsController"/>.
    /// </summary>
    /// <param name="mediator">The MediatR mediator.</param>
    public ProjectsController(IMediator mediator)
    {
        _mediator = mediator;
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
        var command = new CreateProjectCommand(request.Name);
        ProjectResponse response = await _mediator.Send(command, cancellationToken);
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
        ProjectResponse response = await _mediator.Send(new GetProjectQuery(id), cancellationToken);
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
        var command = new AddMemberCommand(id, request.UserId);
        ProjectMemberResponse response = await _mediator.Send(command, cancellationToken);
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
        await _mediator.Send(new RemoveMemberCommand(id, userId), cancellationToken);
        return NoContent();
    }
}
