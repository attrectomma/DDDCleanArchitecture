using Api2.Domain.Entities;

namespace Api2.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="User"/> entities.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 1. User is a reference entity — the repository
/// provides simple lookup and add operations. No eager-loading methods
/// are needed because User has minimal domain behaviour.
/// </remarks>
public interface IUserRepository
{
    /// <summary>Retrieves a user by their unique identifier.</summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user entity, or <c>null</c> if not found.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new user to the repository.</summary>
    /// <param name="user">The user entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
