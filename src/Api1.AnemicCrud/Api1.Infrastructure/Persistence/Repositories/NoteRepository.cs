using Api1.Application.Interfaces;
using Api1.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api1.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="INoteRepository"/>.
/// </summary>
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
    public async Task<bool> ExistsByTextInColumnAsync(
        Guid columnId, string text, CancellationToken cancellationToken = default)
        => await _context.Notes
            .AnyAsync(n => n.ColumnId == columnId && n.Text == text, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Note note, CancellationToken cancellationToken = default)
        => await _context.Notes.AddAsync(note, cancellationToken);

    /// <inheritdoc />
    public void Update(Note note)
        => _context.Notes.Update(note);

    /// <inheritdoc />
    public void Delete(Note note)
        => _context.Notes.Remove(note);
}
