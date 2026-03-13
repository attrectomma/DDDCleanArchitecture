using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;

namespace Api1.Application.Services;

/// <summary>
/// Service contract for retro board operations.
/// </summary>
public interface IRetroBoardService
{
    /// <summary>Creates a new retro board within a project.</summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="request">The retro board creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created retro board response.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the project is not found.</exception>
    Task<RetroBoardResponse> CreateAsync(Guid projectId, CreateRetroBoardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a retro board by its ID, including all columns, notes, and vote counts.</summary>
    /// <param name="id">The retro board ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board response with details.</returns>
    /// <exception cref="Exceptions.NotFoundException">Thrown when the retro board is not found.</exception>
    Task<RetroBoardResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
