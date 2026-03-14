using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;

namespace Api4.Application.Services;

/// <summary>
/// Service contract for project and project membership operations.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3. The Project aggregate is unchanged between
/// API 3 and API 4.
/// </remarks>
public interface IProjectService
{
    /// <summary>Creates a new project.</summary>
    /// <param name="request">The project creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created project response.</returns>
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a project by its ID.</summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The project response.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the project is not found.</exception>
    Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a user as a member to a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="request">The add member request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created membership response.</returns>
    Task<ProjectMemberResponse> AddMemberAsync(Guid projectId, AddMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a user from a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="userId">The user ID to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
}
