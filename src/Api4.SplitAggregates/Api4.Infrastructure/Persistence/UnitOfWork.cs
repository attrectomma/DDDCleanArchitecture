using Api4.Application.Interfaces;

namespace Api4.Infrastructure.Persistence;

/// <summary>
/// Wraps <see cref="RetroBoardDbContext"/>.SaveChangesAsync
/// to provide an explicit unit-of-work boundary.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 1–3. In API 4, because Vote is a separate aggregate,
/// vote operations and retro board operations each call SaveChanges
/// independently — they are separate transactions.
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
