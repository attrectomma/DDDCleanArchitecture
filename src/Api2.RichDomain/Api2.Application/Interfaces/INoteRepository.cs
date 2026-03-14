using Api2.Domain.Entities;

namespace Api2.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="Note"/> entities.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, the Note entity now enforces the one-vote-per-user
/// invariant via <see cref="Note.CastVote"/>. This repository adds
/// <see cref="GetByIdWithVotesAsync"/> to load the note with its votes
/// collection, which the domain method requires. The note text uniqueness
/// check for updates remains a repository query because Note doesn't know
/// about sibling notes.
/// </remarks>
public interface INoteRepository
{
    /// <summary>Retrieves a note by its unique identifier.</summary>
    /// <param name="id">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The note entity, or <c>null</c> if not found.</returns>
    Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a note by its ID, eagerly loading its <see cref="Note.Votes"/> collection.
    /// Required for <see cref="Note.CastVote"/> and <see cref="Note.RemoveVote"/>
    /// domain methods to enforce invariants.
    /// </summary>
    /// <param name="id">The note ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The note entity with its votes, or <c>null</c> if not found.</returns>
    Task<Note?> GetByIdWithVotesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a note with the given text already exists within a column.
    /// </summary>
    /// <param name="columnId">The column ID.</param>
    /// <param name="text">The note text to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if a note with that text exists; otherwise <c>false</c>.</returns>
    Task<bool> ExistsByTextInColumnAsync(Guid columnId, string text, CancellationToken cancellationToken = default);

    /// <summary>Marks a note entity as modified for persistence.</summary>
    /// <param name="note">The note entity to update.</param>
    void Update(Note note);

    /// <summary>Marks a note entity for deletion (soft delete).</summary>
    /// <param name="note">The note entity to delete.</param>
    void Delete(Note note);
}
