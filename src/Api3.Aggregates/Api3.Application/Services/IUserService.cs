using Api3.Application.DTOs.Requests;
using Api3.Application.DTOs.Responses;

namespace Api3.Application.Services;

/// <summary>
/// Service contract for user operations.
/// </summary>
public interface IUserService
{
    /// <summary>Creates a new user.</summary>
    /// <param name="request">The user creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created user response.</returns>
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a user by their ID.</summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user response.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the user is not found.</exception>
    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
