using Api1.Domain.Entities;

namespace Api1.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="Vote"/> entities.
/// </summary>
public interface IVoteRepository
{
    /// <summary>Retrieves a vote by its unique identifier.</summary>
    /// <param name="id">The vote ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The vote entity, or <c>null</c> if not found.</returns>
    Task<Vote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a user has already voted on a given note.
    /// </summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a vote exists for this user/note combination; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(Guid noteId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new vote to the repository.</summary>
    /// <param name="vote">The vote entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(Vote vote, CancellationToken cancellationToken = default);

    /// <summary>Marks a vote entity for deletion (soft delete).</summary>
    /// <param name="vote">The vote entity to delete.</param>
    void Delete(Vote vote);
}
