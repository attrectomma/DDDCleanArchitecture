namespace Api1.Domain.Entities;

/// <summary>
/// Join entity representing the many-to-many relationship between
/// <see cref="Project"/> and <see cref="User"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 this is a first-class entity with its own repository.
/// The 1-to-1 mapping (table → entity → repository → service → controller)
/// is a hallmark of the anemic CRUD approach. API 3+ eliminates this
/// separate entity by managing membership through the Project aggregate.
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
