namespace Api1.Application.Interfaces;

/// <summary>
/// Defines the Unit of Work contract — a single <see cref="SaveChangesAsync"/>
/// call that persists all pending changes tracked by the underlying DbContext.
/// </summary>
/// <remarks>
/// DESIGN: In API 1, each service method calls <see cref="SaveChangesAsync"/>
/// at the end of its operation. There is no transactional coordination across
/// multiple service calls — a deliberate simplification (and weakness) at this tier.
/// The UoW pattern ensures repositories never call SaveChanges themselves;
/// persistence is always an explicit decision by the orchestrating code.
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
