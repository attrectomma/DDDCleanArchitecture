using Api2.Domain.Entities;

namespace Api2.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="RetroBoard"/> entities.
/// </summary>
public interface IRetroBoardRepository
{
    /// <summary>Retrieves a retro board by its unique identifier.</summary>
    /// <param name="id">The retro board ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board entity, or <c>null</c> if not found.</returns>
    Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a retro board by its ID, eagerly loading columns, notes, and votes.
    /// </summary>
    /// <param name="id">The retro board ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board entity with its children, or <c>null</c> if not found.</returns>
    Task<RetroBoard?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new retro board to the repository.</summary>
    /// <param name="retroBoard">The retro board entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(RetroBoard retroBoard, CancellationToken cancellationToken = default);
}
