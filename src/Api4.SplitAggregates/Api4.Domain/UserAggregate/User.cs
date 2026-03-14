using Api4.Domain.Common;

namespace Api4.Domain.UserAggregate;

/// <summary>
/// Represents a user that can participate in retro boards. A simple
/// aggregate root with minimal behaviour.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3. User is treated as its own aggregate root
/// because it has an independent lifecycle — users exist independently of
/// projects and retro boards. User has its own repository
/// (<see cref="IUserRepository"/>).
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
