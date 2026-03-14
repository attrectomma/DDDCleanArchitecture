using Api3.Domain.RetroAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api3.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRetroBoardRepository"/>.
/// Loads the full RetroBoard aggregate in a single query with
/// eager loading of all child entities.
/// </summary>
/// <remarks>
/// DESIGN: We always load the complete aggregate because the
/// aggregate root needs the full state to enforce invariants.
/// This is expensive for large retros — a known trade-off at this tier.
///
/// DESIGN (CQRS foreshadowing): This same expensive query runs for
/// BOTH writes (where the full state is needed for invariants) AND
/// reads (where we only need a DTO). Loading the full aggregate graph
/// with change tracking just to map it to a response is wasteful.
/// API 5 introduces CQRS to separate the read path (lightweight
/// no-tracking projections) from the write path (full aggregate loading).
/// </remarks>
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
        => await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
                    .ThenInclude(n => n.Votes)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(RetroBoard board, CancellationToken cancellationToken = default)
        => await _context.RetroBoards.AddAsync(board, cancellationToken);

    /// <inheritdoc />
    public void Delete(RetroBoard board)
        => _context.RetroBoards.Remove(board);

    /// <inheritdoc />
    public async Task<RetroBoard?> GetByColumnIdAsync(Guid columnId, CancellationToken cancellationToken = default)
        => await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
                    .ThenInclude(n => n.Votes)
            .FirstOrDefaultAsync(r => r.Columns.Any(c => c.Id == columnId), cancellationToken);

    /// <inheritdoc />
    public async Task<RetroBoard?> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default)
        => await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
                    .ThenInclude(n => n.Votes)
            .FirstOrDefaultAsync(r => r.Columns.Any(c => c.Notes.Any(n => n.Id == noteId)), cancellationToken);
}
