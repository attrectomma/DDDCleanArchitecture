using Api5.Domain.VoteAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api5.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IVoteRepository"/>.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 4, minus the <c>GetVoteCountsByNoteIdsAsync</c> method.
/// In API 5, vote counts are computed by CQRS query handlers via correlated
/// subqueries in database projections — no need for a separate repository method.
///
/// The <see cref="GetByNoteIdAsync"/> method is retained because it is used
/// by the <c>NoteRemovedEventHandler</c> to clean up orphaned votes — a
/// write-side operation triggered by a domain event.
/// </remarks>
public class VoteRepository : IVoteRepository
{
    private readonly RetroBoardDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="VoteRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public VoteRepository(RetroBoardDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Vote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Votes.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid noteId, Guid userId, CancellationToken cancellationToken = default)
        => await _context.Votes.AnyAsync(
            v => v.NoteId == noteId && v.UserId == userId,
            cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Vote vote, CancellationToken cancellationToken = default)
        => await _context.Votes.AddAsync(vote, cancellationToken);

    /// <inheritdoc />
    public void Delete(Vote vote)
        => _context.Votes.Remove(vote);

    /// <inheritdoc />
    public async Task<List<Vote>> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default)
        => await _context.Votes
            .Where(v => v.NoteId == noteId)
            .ToListAsync(cancellationToken);
}
