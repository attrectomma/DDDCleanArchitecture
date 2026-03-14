using Api2.Domain.Entities;

namespace Api2.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="Vote"/> entities.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, vote creation is handled by <see cref="Note.CastVote"/>
/// and vote removal by <see cref="Note.RemoveVote"/>, so this repository
/// is only used as a fallback for direct vote lookup. Most vote operations
/// now flow through the Note entity's domain methods. In API 3+ this
/// repository may be eliminated entirely when the aggregate root manages
/// the full entity graph.
/// </remarks>
public interface IVoteRepository
{
    /// <summary>Retrieves a vote by its unique identifier.</summary>
    /// <param name="id">The vote ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The vote entity, or <c>null</c> if not found.</returns>
    Task<Vote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Marks a vote entity for deletion (soft delete).</summary>
    /// <param name="vote">The vote entity to delete.</param>
    void Delete(Vote vote);
}
