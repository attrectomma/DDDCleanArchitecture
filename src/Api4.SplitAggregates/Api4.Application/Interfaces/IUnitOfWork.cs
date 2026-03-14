namespace Api4.Application.Interfaces;

/// <summary>
/// Defines the Unit of Work contract — a single <see cref="SaveChangesAsync"/>
/// call that persists all pending changes tracked by the underlying DbContext.
/// </summary>
/// <remarks>
/// DESIGN: Same as API 1–3. In API 4, each service method calls
/// <see cref="SaveChangesAsync"/> at the end of its operation. Because
/// Vote is now a separate aggregate, vote operations and retro board
/// operations each call SaveChanges independently — they are separate
/// transactions, which is the fundamental trade-off of splitting aggregates.
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
