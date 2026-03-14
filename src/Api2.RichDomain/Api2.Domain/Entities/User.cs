namespace Api2.Domain.Entities;

/// <summary>
/// Represents a user that can participate in retro boards.
/// </summary>
/// <remarks>
/// DESIGN: Unlike API 1 where User was a plain property bag, API 2's User
/// entity uses a factory constructor and private setters. This ensures that
/// a User can only be created with valid data. However, User has minimal
/// domain behaviour because it is mostly a reference entity — the real
/// business logic lives in <see cref="Project"/> (membership),
/// <see cref="Column"/> (notes), and <see cref="Note"/> (votes).
/// </remarks>
public class User : AuditableEntityBase
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

    /// <summary>Gets the collection of project memberships for this user.</summary>
    public ICollection<ProjectMember> ProjectMemberships { get; private set; } = new List<ProjectMember>();
}
