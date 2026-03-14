using Api2.Application.Interfaces;
using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api2.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRetroBoardRepository"/>.
/// </summary>
public class RetroBoardRepository : IRetroBoardRepository
{
    private readonly RetroBoardDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public RetroBoardRepository(RetroBoardDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<RetroBoard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.RetroBoards.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<RetroBoard?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
                    .ThenInclude(n => n.Votes)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(RetroBoard retroBoard, CancellationToken cancellationToken = default)
        => await _context.RetroBoards.AddAsync(retroBoard, cancellationToken);
}
