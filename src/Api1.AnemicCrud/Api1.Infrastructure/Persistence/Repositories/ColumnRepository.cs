using Api1.Application.Interfaces;
using Api1.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api1.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IColumnRepository"/>.
/// </summary>
public class ColumnRepository : IColumnRepository
{
    private readonly RetroBoardDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ColumnRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public ColumnRepository(RetroBoardDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Column?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Columns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> ExistsByNameInRetroAsync(
        Guid retroBoardId, string name, CancellationToken cancellationToken = default)
        => await _context.Columns
            .AnyAsync(c => c.RetroBoardId == retroBoardId && c.Name == name, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Column column, CancellationToken cancellationToken = default)
        => await _context.Columns.AddAsync(column, cancellationToken);

    /// <inheritdoc />
    public void Update(Column column)
        => _context.Columns.Update(column);

    /// <inheritdoc />
    public void Delete(Column column)
        => _context.Columns.Remove(column);
}
