namespace Api5.Application.Common.Interfaces;

/// <summary>
/// Defines the Unit of Work contract — a single <see cref="SaveChangesAsync"/>
/// call that persists all pending changes tracked by the underlying DbContext.
/// </summary>
/// <remarks>
/// DESIGN: Same contract as API 1–4. In API 5, command handlers call
/// <see cref="SaveChangesAsync"/> at the end of their operation.
///
/// The <see cref="Behaviors.TransactionBehavior{TRequest, TResponse}"/>
/// pipeline behavior wraps command execution in an explicit database
/// transaction. When a transaction is already open, EF Core’s
/// <c>SaveChangesAsync</c> does NOT create its own implicit transaction —
/// it flushes changes within the existing one. So the Unit of Work
/// continues working exactly as before; it just participates in the
/// transaction that <c>TransactionBehavior</c> opened.
///
/// <list type="bullet">
///   <item><see cref="IUnitOfWork"/> = “flush tracked changes” (data level).</item>
///   <item><c>TransactionBehavior</c> = “everything is atomic” (pipeline level).</item>
/// </list>
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
