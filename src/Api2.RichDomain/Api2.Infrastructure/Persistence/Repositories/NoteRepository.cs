using Api2.Application.Interfaces;
using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api2.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="INoteRepository"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, <see cref="GetByIdWithVotesAsync"/> is new — it loads
/// the note with its votes collection so that <see cref="Note.CastVote"/>
/// and <see cref="Note.RemoveVote"/> can enforce invariants in-memory.
/// </remarks>
public class NoteRepository : INoteRepository
{
    private readonly RetroBoardDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="NoteRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public NoteRepository(RetroBoardDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Note?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Notes.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Note?> GetByIdWithVotesAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Notes
            .Include(n => n.Votes)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByTextInColumnAsync(
        Guid columnId, string text, CancellationToken cancellationToken = default)
        => await _context.Notes
            .AnyAsync(n => n.ColumnId == columnId && n.Text == text, cancellationToken);

    /// <inheritdoc />
    public void Update(Note note)
        => _context.Notes.Update(note);

    /// <inheritdoc />
    public void Delete(Note note)
        => _context.Notes.Remove(note);
}
