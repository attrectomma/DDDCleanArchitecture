using Api1.Domain.Entities;

namespace Api1.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="Note"/> entities.
/// </summary>
public interface INoteRepository
{
    /// <summary>Retrieves a note by its unique identifier.</summary>
    /// <param name="id">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The note entity, or <c>null</c> if not found.</returns>
    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a note with the given text already exists within a column.
    /// </summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="text">The note text to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a note with that text exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsByTextInColumnAsync(Guid columnId, string text, CancellationToken cancellationToken = default);

    /// <summary>Adds a new note to the repository.</summary>
    /// <param name="note">The note entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(Note note, CancellationToken cancellationToken = default);

    /// <summary>Marks a note entity as modified for persistence.</summary>
    /// <param name="note">The note entity to update.</param>
    void Update(Note note);

    /// <summary>Marks a note entity for deletion (soft delete).</summary>
    /// <param name="note">The note entity to delete.</param>
    void Delete(Note note);
}
