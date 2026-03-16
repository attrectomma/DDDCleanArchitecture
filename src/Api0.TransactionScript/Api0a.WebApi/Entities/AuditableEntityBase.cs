namespace Api0a.WebApi.Entities;

/// <summary>
/// Base class for all auditable entities. Provides tracking for creation,
/// modification, and soft-delete timestamps.
/// </summary>
/// <remarks>
/// DESIGN: Same shape as API 1's <c>AuditableEntityBase</c>. Timestamps are
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

    /// <summary>
    /// Computed property indicating whether this entity has been soft-deleted.
    /// </summary>
    public bool IsDeleted => DeletedAt.HasValue;
}
