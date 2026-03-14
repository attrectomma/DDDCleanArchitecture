using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;

namespace Api2.Application.Services;

/// <summary>
/// Service contract for project-related operations.
/// </summary>
public interface IProjectService
{
    /// <summary>Creates a new project.</summary>
    /// <param name="request">The project creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created project response.</returns>
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a project by its unique identifier.</summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The project response.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the project is not found.</exception>
    Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
