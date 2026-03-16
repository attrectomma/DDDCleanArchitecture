namespace Api0a.WebApi.Entities;

/// <summary>
/// Represents a user that can participate in retro boards.
/// </summary>
/// <remarks>
/// DESIGN: Anemic entity — public setters, no validation, no domain logic.
/// In the Transaction Script pattern, all business rules live in the endpoint
/// handlers. Compare with API 2+ where entities own their invariants.
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
