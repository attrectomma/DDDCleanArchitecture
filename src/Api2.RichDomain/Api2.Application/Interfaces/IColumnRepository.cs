using Api2.Domain.Entities;

namespace Api2.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="Column"/> entities.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, the Column entity now enforces note text uniqueness
/// via <see cref="Column.AddNote"/>. This repository adds
/// <see cref="GetByIdWithNotesAsync"/> to load the column with its notes
/// collection, which the domain method requires. The column name uniqueness
/// check across a retro board remains a repository query because the Column
/// entity doesn't know about its siblings.
/// </remarks>
public interface IColumnRepository
{
    /// <summary>Retrieves a column by its unique identifier.</summary>
    /// <param name="id">The column ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The column entity, or <c>null</c> if not found.</returns>
    Task<Column?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a column by its ID, eagerly loading its <see cref="Column.Notes"/> collection.
    /// Required for <see cref="Column.AddNote"/> to enforce the unique-text invariant.
    /// </summary>
    /// <param name="id">The column ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The column entity with its notes, or <c>null</c> if not found.</returns>
    Task<Column?> GetByIdWithNotesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a column with the given name already exists within a retro board.
    /// </summary>
    /// <param name="retroBoardId">The retro board ID.</param>
    /// <param name="name">The column name to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a column with that name exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsByNameInRetroAsync(Guid retroBoardId, string name, CancellationToken cancellationToken = default);

    /// <summary>Adds a new column to the repository.</summary>
    /// <param name="column">The column entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(Column column, CancellationToken cancellationToken = default);

    /// <summary>Marks a column entity as modified for persistence.</summary>
    /// <param name="column">The column entity to update.</param>
    void Update(Column column);

    /// <summary>Marks a column entity for deletion (soft delete).</summary>
    /// <param name="column">The column entity to delete.</param>
    void Delete(Column column);
}
