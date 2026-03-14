namespace Api2.Application.Interfaces;

/// <summary>
/// Defines the Unit of Work contract — a single <see cref="SaveChangesAsync"/>
/// call that persists all pending changes tracked by the underlying DbContext.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 1. Each service method calls <see cref="SaveChangesAsync"/>
/// at the end of its operation. In API 2 the entity methods modify in-memory state;
/// the service orchestrator calls SaveChanges to flush everything to the database.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
