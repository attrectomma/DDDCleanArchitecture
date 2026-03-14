using Api4.Domain.RetroAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api4.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IRetroBoardRepository"/>.
/// Loads the RetroBoard aggregate with columns and notes but WITHOUT votes.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 3's repository that loaded
/// <c>.ThenInclude(n => n.Votes)</c>. Removing that ThenInclude means:
///   - Faster aggregate loading.
///   - Smaller memory footprint.
///   - No write contention between note edits and vote operations.
///
/// A new <see cref="NoteExistsAsync"/> method provides lightweight
/// cross-aggregate validation for VoteService.
///
/// DESIGN (CQRS foreshadowing): Even though we split Vote out (reducing
/// load size), we still load the full RetroBoard aggregate for EVERY
/// operation — including GET requests that only need a read-only view.
/// API 5's CQRS pattern addresses this: queries bypass the aggregate
/// entirely and project directly from the database with no-tracking.
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
            // NO votes — separate aggregate
            .FirstOrDefaultAsync(r => r.Columns.Any(c => c.Id == columnId), cancellationToken);

    /// <inheritdoc />
    public async Task<RetroBoard?> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default)
        => await _context.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
            // NO votes — separate aggregate
            .FirstOrDefaultAsync(r => r.Columns.Any(c => c.Notes.Any(n => n.Id == noteId)), cancellationToken);

    /// <inheritdoc />
    public async Task<bool> NoteExistsAsync(Guid noteId, CancellationToken cancellationToken = default)
        => await _context.Set<Note>()
            .AnyAsync(n => n.Id == noteId && n.DeletedAt == null, cancellationToken);
}
