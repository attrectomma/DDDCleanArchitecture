using Api4.Domain.VoteAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api4.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IVoteRepository"/>.
/// </summary>
/// <remarks>
/// DESIGN: This repository is NEW in API 4. In API 3, there was no
/// VoteRepository because Vote was a child entity within the RetroBoard
/// aggregate. Now that Vote is its own aggregate root, it has a dedicated
/// repository for CRUD operations and cross-aggregate query support.
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

    /// <inheritdoc />
    /// <remarks>
    /// DESIGN: This batch query avoids the N+1 problem when loading vote counts
    /// for all notes in a retro board. Instead of querying per-note, we do a
    /// single GROUP BY query. This is a necessary workaround because votes are
    /// no longer part of the RetroBoard aggregate — the cost of splitting.
    /// </remarks>
    public async Task<Dictionary<Guid, int>> GetVoteCountsByNoteIdsAsync(
        IEnumerable<Guid> noteIds,
        CancellationToken cancellationToken = default)
    {
        List<Guid> noteIdList = noteIds.ToList();

        return await _context.Votes
            .Where(v => noteIdList.Contains(v.NoteId))
            .GroupBy(v => v.NoteId)
            .Select(g => new { NoteId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.NoteId, x => x.Count, cancellationToken);
    }
}
