namespace Api2.Domain.Entities;

/// <summary>
/// Base class for all auditable entities. Provides tracking for creation,
/// modification, and soft-delete timestamps.
/// </summary>
/// <remarks>
/// DESIGN: Timestamps are populated by the EF Core <c>AuditInterceptor</c>
/// so entities remain unaware of auditing concerns. This base class is
/// identical across all five APIs — the audit shape does not change.
///
/// In API 2 the derived entities add rich behaviour (private setters,
/// factory constructors, domain methods) while this base class stays the same.
/// The base properties use public setters because the <c>AuditInterceptor</c>
/// needs to set them directly.
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
