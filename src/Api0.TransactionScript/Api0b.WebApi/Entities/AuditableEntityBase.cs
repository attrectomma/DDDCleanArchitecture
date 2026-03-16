namespace Api0b.WebApi.Entities;

/// <summary>
/// Base class for all auditable entities. Provides tracking for creation,
/// modification, and soft-delete timestamps.
/// </summary>
/// <remarks>
/// DESIGN: Same shape as Api0a's <c>AuditableEntityBase</c>. Timestamps are
/// populated by the <see cref="Data.Interceptors.AuditInterceptor"/> so
/// entities remain unaware of auditing. In the Transaction Script pattern
/// entities are pure property bags — EF Core mapping targets and nothing more.
/// There is no separate Domain project because there is no "domain layer."
/// </remarks>
public abstract class AuditableEntityBase
{
    /// <summary>Gets or sets the unique identifier for this entity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this entity was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp when this entity was last modified.</summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this entity was soft-deleted.
    /// <c>null</c> indicates the entity is active.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
