using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;

namespace Api2.Application.Services;

/// <summary>
/// Service contract for project membership operations.
/// </summary>
public interface IProjectMemberService
{
    /// <summary>Adds a user as a member to a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="request">The add member request containing the user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created membership response.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the project or user is not found.</exception>
    /// <exception cref="Api2.Domain.Exceptions.InvariantViolationException">
    /// Thrown when the user is already a member.
    /// </exception>
    Task<ProjectMemberResponse> AddMemberAsync(Guid projectId, AddMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a user from a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="userId">The user ID to remove.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the membership is not found.</exception>
    Task RemoveMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
}
