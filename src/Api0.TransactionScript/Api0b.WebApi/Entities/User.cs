namespace Api0b.WebApi.Entities;

/// <summary>
/// Represents a user that can participate in retro boards.
/// </summary>
/// <remarks>
/// DESIGN: Same anemic entity as Api0a, with one addition: the
/// <see cref="Version"/> property is mapped to PostgreSQL's <c>xmin</c>
/// system column. This enables EF Core optimistic concurrency detection —
/// if two concurrent requests try to modify the same user, the second one
/// will fail with a <c>DbUpdateConcurrencyException</c> caught by the
/// middleware. No endpoint handler code needed to change.
/// </remarks>
public class User : AuditableEntityBase
{
    /// <summary>Gets or sets the display name of the user.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address of the user.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection of project memberships for this user.</summary>
    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();

    /// <summary>
    /// Gets or sets the concurrency token mapped to PostgreSQL's <c>xmin</c> system column.
    /// Used by EF Core for optimistic concurrency detection.
    /// </summary>
    public uint Version { get; set; }
}
