namespace Api1.Domain.Entities;

/// <summary>
/// Base class for all auditable entities. Provides tracking for creation,
/// modification, and soft-delete timestamps.
/// </summary>
/// <remarks>
/// DESIGN: Timestamps are populated by the EF Core <c>AuditInterceptor</c>
/// so entities remain unaware of auditing concerns. In API 1 (anemic model)
/// entities carry no behaviour — they are pure property bags.
///
/// All five APIs share this same base class shape, but API 2+ add behaviour
/// methods on derived entities while keeping the base class unchanged.
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
