using Api0a.WebApi.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Api0a.WebApi.Data.Interceptors;

/// <summary>
/// EF Core interceptor that stamps <see cref="AuditableEntityBase.CreatedAt"/>
/// and <see cref="AuditableEntityBase.LastUpdatedAt"/> on save, and converts
/// Delete operations into soft deletes by setting <see cref="AuditableEntityBase.DeletedAt"/>.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 1's <c>AuditInterceptor</c>. By using an interceptor,
/// auditing and soft-delete logic is centralised in one place rather than repeated
/// in every endpoint handler. This is one of the few cross-cutting abstractions
/// the Transaction Script pattern retains — it would be impractical to duplicate
/// this logic in every endpoint.
/// </remarks>
public class AuditInterceptor : SaveChangesInterceptor
{
    /// <summary>
    /// Intercepts the <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    /// pipeline to stamp audit timestamps and convert deletes to soft deletes.
    /// </summary>
    /// <param name="eventData">The event data containing the DbContext.</param>
    /// <param name="result">The current interception result.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The interception result.</returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<AuditableEntityBase>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.LastUpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastUpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    // Convert hard delete into soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.LastUpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
