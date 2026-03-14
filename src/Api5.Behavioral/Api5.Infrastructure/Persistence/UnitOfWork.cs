using Api5.Application.Common.Interfaces;

namespace Api5.Infrastructure.Persistence;

/// <summary>
/// Wraps <see cref="RetroBoardDbContext"/>.SaveChangesAsync
/// to provide an explicit unit-of-work boundary.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 1–4. In API 5, command handlers call SaveChanges
/// at the end of their operation. Domain event handlers triggered by the
/// <see cref="Interceptors.DomainEventInterceptor"/> may also call
/// SaveChanges to persist their side effects (e.g., vote cleanup).
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
