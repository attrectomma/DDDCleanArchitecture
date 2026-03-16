namespace Api0a.WebApi.Entities;

/// <summary>
/// Join entity representing the many-to-many relationship between
/// <see cref="Project"/> and <see cref="User"/>.
/// </summary>
/// <remarks>
/// DESIGN: In the Transaction Script pattern this is just a row in a join
/// table. There is no dedicated repository or service for it — the endpoint
/// handler queries and mutates the DbSet directly.
/// </remarks>
public class ProjectMember : AuditableEntityBase
{
    /// <summary>Gets or sets the ID of the project.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the ID of the user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the navigation property to the project.</summary>
    public Project Project { get; set; } = null!;

    /// <summary>Gets or sets the navigation property to the user.</summary>
    public User User { get; set; } = null!;
}
