namespace Api5.Domain.UserAggregate;

/// <summary>
/// Repository contract for the <see cref="User"/> aggregate root.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 3/4. Repository interfaces live in the Domain layer.
/// In API 5, this repository is used only by the <c>CreateUserCommandHandler</c>
/// (write side). The <c>GetUserQueryHandler</c> (read side) bypasses the
/// repository entirely and queries <c>DbContext</c> directly — this is the
/// CQRS split in action.
/// </remarks>
public interface IUserRepository
{
    /// <summary>Retrieves a user by ID.</summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user, or <c>null</c> if not found.</returns>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new user to the repository.</summary>
    /// <param name="user">The user to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
