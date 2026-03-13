namespace Api1.Domain.Entities;

/// <summary>
/// Represents a user that can participate in retro boards.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 this entity is intentionally anemic — public setters,
/// no validation methods, no domain logic. All business rules (e.g.,
/// checking membership before voting) live in the Service layer.
/// See API 2 where validation logic moves into the entity itself.
/// </remarks>
public class User : AuditableEntityBase
{
    /// <summary>Gets or sets the display name of the user.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection of project memberships for this user.</summary>
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
}
