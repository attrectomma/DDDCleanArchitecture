using Api1.Domain.Entities;

namespace Api1.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="User"/> entities.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 every entity has its own repository interface and
/// implementation (1-to-1 mapping). This is the classic anemic CRUD pattern
/// where repositories are thin wrappers around DbSet operations.
/// API 3+ eliminate per-entity repositories in favour of aggregate-level repositories.
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
