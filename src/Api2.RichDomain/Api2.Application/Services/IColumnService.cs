using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;

namespace Api2.Application.Services;

/// <summary>
/// Service contract for column-related operations.
/// </summary>
public interface IColumnService
{
    /// <summary>Creates a new column in a retro board.</summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="request">The column creation request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created column response.</returns>
    Task<ColumnResponse> CreateAsync(Guid retroBoardId, CreateColumnRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing column's name.</summary>
    /// <param name="retroBoardId">The retro board ID (for route context).</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="request">The column update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated column response.</returns>
    Task<ColumnResponse> UpdateAsync(Guid retroBoardId, Guid columnId, UpdateColumnRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a column.</summary>
    /// <param name="retroBoardId">The retro board ID (for route context).</param>
    /// <param name="columnId">The column ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task DeleteAsync(Guid retroBoardId, Guid columnId, CancellationToken cancellationToken = default);
}
