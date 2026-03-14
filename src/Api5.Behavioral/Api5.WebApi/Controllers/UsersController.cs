using Api5.Application.DTOs.Requests;
using Api5.Application.DTOs.Responses;
using Api5.Application.Users.Commands.CreateUser;
using Api5.Application.Users.Queries.GetUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api5.WebApi.Controllers;

/// <summary>
/// Controller for user management operations.
/// Dispatches commands and queries via <see cref="IMediator"/>.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 4's UsersController that depended on
/// <c>IUserService</c>. Here, the controller depends only on
/// <see cref="IMediator"/> and doesn't know what handler will process
/// the request. This decoupling makes it trivial to add new operations
/// without modifying the controller's constructor.
/// </remarks>
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of <see cref="UsersController"/>.
    /// </summary>
    /// <param name="mediator">The MediatR mediator.</param>
    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Creates a new user.</summary>
    /// <param name="request">The user creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created user.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Name, request.Email);
        UserResponse response = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Retrieves a user by their ID.</summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        UserResponse response = await _mediator.Send(new GetUserQuery(id), cancellationToken);
        return Ok(response);
    }
}
