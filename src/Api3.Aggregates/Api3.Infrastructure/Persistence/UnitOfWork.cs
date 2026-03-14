using Api3.Application.Interfaces;

namespace Api3.Infrastructure.Persistence;

/// <summary>
/// Wraps <see cref="RetroBoardDbContext"/>.SaveChangesAsync
/// to provide an explicit unit-of-work boundary.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 1/2. In API 3, because the aggregate root is the
/// consistency boundary, a single <see cref="SaveChangesAsync"/> call
/// persists the entire atomic operation — all modifications to the aggregate
/// (root + children) are flushed in one transaction.
/// </remarks>
public class UnitOfWork : IUnitOfWork
{
    private readonly RetroBoardDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="UnitOfWork"/>.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public UnitOfWork(RetroBoardDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
