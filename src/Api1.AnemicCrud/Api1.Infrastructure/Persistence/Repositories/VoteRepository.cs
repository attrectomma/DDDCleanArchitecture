using Api1.Application.Interfaces;
using Api1.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api1.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IVoteRepository"/>.
/// </summary>
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
        => await _context.Votes
            .AnyAsync(v => v.NoteId == noteId && v.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Vote vote, CancellationToken cancellationToken = default)
        => await _context.Votes.AddAsync(vote, cancellationToken);

    /// <inheritdoc />
    public void Delete(Vote vote)
        => _context.Votes.Remove(vote);
}
