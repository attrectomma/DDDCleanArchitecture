using Api5.Domain.Common;

namespace Api5.Domain.UserAggregate;

/// <summary>
/// Represents a user that can participate in retro boards. A simple
/// aggregate root with minimal behaviour.
/// </summary>
/// <remarks>
/// DESIGN: Same structure as API 3/4. User inherits from <see cref="AggregateRoot"/>
/// (new in API 5) instead of directly from <see cref="AuditableEntityBase"/>.
/// This gives User the ability to raise domain events, although User
/// currently does not raise any. The aggregate boundary and behaviour
/// are unchanged — the key API 5 change is in how controllers interact
/// with aggregates (via MediatR commands/queries, not services).
/// </remarks>
public class User : AggregateRoot
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
