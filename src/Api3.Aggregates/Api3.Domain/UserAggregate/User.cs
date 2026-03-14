using Api3.Domain.Common;

namespace Api3.Domain.UserAggregate;

/// <summary>
/// Represents a user that can participate in retro boards. A simple
/// aggregate root with minimal behaviour.
/// </summary>
/// <remarks>
/// DESIGN: User is treated as its own aggregate root in API 3 because
/// it has an independent lifecycle — users exist independently of projects
/// and retro boards. User has its own repository (<see cref="IUserRepository"/>).
///
/// Unlike the Project and RetroBoard aggregates, User has no child entities.
/// It is effectively a reference entity that other aggregates point to by ID.
/// </remarks>
public class User : AuditableEntityBase, IAggregateRoot
{
    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private User() { }

    /// <summary>
    /// Creates a new user with the specified name and email.
    /// </summary>
    /// <param name="name">The display name of the user.</param>
    /// <param name="email">The email address of the user.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="email"/> is null, empty, or whitespace.
    /// </exception>
    public User(string name, string email)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
        Email = Guard.AgainstNullOrWhiteSpace(email, nameof(email));
    }

    /// <summary>Gets the display name of the user.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the email address of the user.</summary>
    public string Email { get; private set; } = string.Empty;
}
