using Api2.Application.Interfaces;
using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api2.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IVoteRepository"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, most vote operations flow through <see cref="Note.CastVote"/>
/// and <see cref="Note.RemoveVote"/>. This repository is kept for direct vote
/// lookup when needed but is significantly less used than in API 1.
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
    public void Delete(Vote vote)
        => _context.Votes.Remove(vote);
}
