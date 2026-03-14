namespace Api5.Application.Common.Interfaces;

/// <summary>
/// Defines the Unit of Work contract — a single <see cref="SaveChangesAsync"/>
/// call that persists all pending changes tracked by the underlying DbContext.
/// </summary>
/// <remarks>
/// DESIGN: Same contract as API 1–4. In API 5, command handlers call
/// <see cref="SaveChangesAsync"/> at the end of their operation. The
/// <c>TransactionBehavior</c> pipeline behavior wraps command execution
/// in an explicit transaction so domain events dispatched after save
/// participate in the same transaction scope.
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
