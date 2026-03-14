using Api2.Application.Interfaces;

namespace Api2.Infrastructure.Persistence;

/// <summary>
/// Wraps <see cref="RetroBoardDbContext"/>.SaveChangesAsync
/// to provide an explicit unit-of-work boundary.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 1. Each service calls <see cref="SaveChangesAsync"/>
/// at the end of its method. In API 2, domain entity methods modify
/// in-memory state (add/remove from collections, update properties);
/// the UoW flushes all tracked changes to the database in a single call.
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
