namespace Api2.Domain.Entities;

/// <summary>
/// Join entity representing the many-to-many relationship between
/// <see cref="Project"/> and <see cref="User"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 this entity had public setters and was managed by a
/// dedicated <c>ProjectMemberRepository</c> + <c>ProjectMemberService</c>.
/// In API 2, instances are created by <see cref="Project.AddMember"/> and
/// removed by <see cref="Project.RemoveMember"/>. The separate repository
/// and service for this join entity are no longer needed — changes flow
/// through the Project entity and are detected by EF Core's change tracker.
///
/// API 3+ eliminate this pattern entirely by managing membership through
/// the Project aggregate root.
/// </remarks>
public class ProjectMember : AuditableEntityBase
{
    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private ProjectMember() { }

    /// <summary>
    /// Creates a new project membership.
    /// </summary>
    /// <param name="projectId">The ID of the project.</param>
    /// <param name="userId">The ID of the user.</param>
    public ProjectMember(Guid projectId, Guid userId)
    {
        ProjectId = projectId;
        UserId = userId;
    }

    /// <summary>Gets the ID of the project.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Gets the ID of the user.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the navigation property to the project.</summary>
    public Project Project { get; private set; } = null!;

    /// <summary>Gets the navigation property to the user.</summary>
    public User User { get; private set; } = null!;
}
