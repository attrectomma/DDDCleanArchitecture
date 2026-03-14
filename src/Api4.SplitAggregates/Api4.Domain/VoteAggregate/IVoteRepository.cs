namespace Api4.Domain.VoteAggregate;

/// <summary>
/// Repository contract for the <see cref="Vote"/> aggregate root.
/// </summary>
/// <remarks>
/// DESIGN: This repository is NEW in API 4. In API 3, there was no
/// <c>IVoteRepository</c> because Vote was a child entity within the
/// RetroBoard aggregate. Now that Vote is its own aggregate root,
/// it needs its own repository for persistence operations.
///
/// The <see cref="ExistsAsync"/> method supports the application-level
/// duplicate vote check in VoteService. The DB unique constraint on
/// (NoteId, UserId) is the ultimate safety net.
/// </remarks>
public interface IVoteRepository
{
    /// <summary>Retrieves a vote by its unique identifier.</summary>
    /// <param name="id">The vote's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The vote, or <c>null</c> if not found.</returns>
    Task<Vote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a vote already exists for the given note and user combination.
    /// </summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a vote exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsAsync(Guid noteId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a new vote to the repository.</summary>
    /// <param name="vote">The vote to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(Vote vote, CancellationToken cancellationToken = default);

    /// <summary>Marks a vote for deletion.</summary>
    /// <param name="vote">The vote to delete.</param>
    void Delete(Vote vote);

    /// <summary>
    /// Retrieves all votes for a given note.
    /// Used for building vote counts in read operations.
    /// </summary>
    /// <param name="noteId">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The list of votes for the note.</returns>
    Task<List<Vote>> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets vote counts grouped by note ID for a set of note IDs.
    /// Used by RetroBoardService to populate vote counts in GET responses
    /// without loading Vote aggregates individually.
    /// </summary>
    /// <param name="noteIds">The note IDs to get vote counts for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A dictionary mapping note ID to vote count.</returns>
    /// <remarks>
    /// DESIGN: This method exists because Vote is now a separate aggregate.
    /// In API 3, vote counts were available via <c>note.Votes.Count</c> because
    /// the RetroBoard aggregate loaded everything. Now we need an explicit
    /// cross-aggregate read query. This awkwardness is a cost of splitting
    /// the aggregate — and a CQRS foreshadowing moment: API 5 handles this
    /// with a dedicated query handler that joins tables directly.
    /// </remarks>
    Task<Dictionary<Guid, int>> GetVoteCountsByNoteIdsAsync(
        IEnumerable<Guid> noteIds,
        CancellationToken cancellationToken = default);
}
