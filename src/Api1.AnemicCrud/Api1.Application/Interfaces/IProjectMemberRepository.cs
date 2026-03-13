using Api1.Domain.Entities;

namespace Api1.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="ProjectMember"/> join entities.
/// </summary>
/// <remarks>
/// DESIGN: A dedicated repository for a join entity is typical of the
/// anemic 1-to-1 mapping in API 1. API 3+ manage membership through
/// the Project aggregate — no separate ProjectMember repository exists.
/// </remarks>
public interface IProjectMemberRepository
{
    /// <summary>
    /// Checks whether a user is already a member of a project.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the membership already exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new project membership.</summary>
    /// <param name="member">The membership entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a project membership by project and user IDs.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The membership entity, or <c>null</c> if not found.</returns>
    Task<ProjectMember?> GetByProjectAndUserAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Marks a project membership for deletion.</summary>
    /// <param name="member">The membership entity to delete.</param>
    void Delete(ProjectMember member);
}
