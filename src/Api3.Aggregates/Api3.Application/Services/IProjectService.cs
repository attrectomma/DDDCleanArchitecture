using Api3.Application.DTOs.Requests;
using Api3.Application.DTOs.Responses;

namespace Api3.Application.Services;

/// <summary>
/// Service contract for project and project membership operations.
/// </summary>
/// <remarks>
/// DESIGN: In API 3 there is a single <see cref="IProjectService"/> that
/// handles both project CRUD and membership operations. In API 2 these
/// were split into <c>IProjectService</c> and <c>IProjectMemberService</c>.
/// Since the Project aggregate root owns membership, a single service
/// covers the entire aggregate's use cases.
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
    /// <exception cref="Exceptions.NotFoundException">Thrown when the project or user is not found.</exception>
    Task<ProjectMemberResponse> AddMemberAsync(Guid projectId, AddMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a user from a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="userId">The user ID to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the project is not found or the user is not a member.</exception>
    Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
}
