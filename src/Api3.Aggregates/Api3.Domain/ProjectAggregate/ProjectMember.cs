using Api3.Domain.Common;

namespace Api3.Domain.ProjectAggregate;

/// <summary>
/// Join entity representing the many-to-many relationship between
/// a <see cref="Project"/> and a User. Owned by the Project aggregate.
/// </summary>
/// <remarks>
/// DESIGN: In API 1/2, ProjectMember had its own repository and/or was
/// managed at the service level. In API 3, ProjectMember is a child entity
/// within the Project aggregate — it has no repository. Instances are
/// created by <see cref="Project.AddMember"/> and removed by
/// <see cref="Project.RemoveMember"/>. The Project aggregate root owns
/// all membership mutations.
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
}
