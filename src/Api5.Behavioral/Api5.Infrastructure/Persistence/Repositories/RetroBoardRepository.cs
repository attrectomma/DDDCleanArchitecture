using Api5.Domain.RetroAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api5.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRetroBoardRepository"/>.
/// Loads the RetroBoard aggregate with columns and notes but WITHOUT votes.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 4. In API 5, this repository is used ONLY
/// by command handlers (write side). Query handlers bypass the repository
/// and project directly from <c>IReadOnlyDbContext</c>.
///
/// This is the CQRS insight materialised: the same expensive eager-loading
/// query that API 4 ran for both reads and writes now runs only for writes
/// where it is actually needed (to enforce invariants).
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
            // NO .ThenInclude(n => n.Votes) — votes are a separate aggregate
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
            .FirstOrDefaultAsync(r => r.Columns.Any(c => c.Id == columnId), cancellationToken);

    /// <inheritdoc />
    public async Task<RetroBoard?> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default)
        => await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
            .FirstOrDefaultAsync(r => r.Columns.Any(c => c.Notes.Any(n => n.Id == noteId)), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> NoteExistsAsync(Guid noteId, CancellationToken cancellationToken = default)
        => await _context.Set<Note>()
            .AnyAsync(n => n.Id == noteId && n.DeletedAt == null, cancellationToken);
}
