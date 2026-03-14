using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;
using Api4.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api4.WebApi.Controllers;

/// <summary>
/// Controller for user management operations.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3. The User aggregate is unchanged.
/// </remarks>
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    /// <summary>
    /// Initializes a new instance of <see cref="UsersController"/>.
    /// </summary>
    /// <param name="userService">The user service.</param>
    public UsersController(IUserService userService)
    {
        _userService = userService;
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
        UserResponse response = await _userService.CreateAsync(request, cancellationToken);
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
        UserResponse response = await _userService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }
}
