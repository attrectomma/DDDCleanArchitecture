namespace Api5.Domain.Common;

/// <summary>
/// Base class for all auditable entities. Provides tracking for creation,
/// modification, and soft-delete timestamps.
/// </summary>
/// <remarks>
/// DESIGN: Timestamps are populated by the EF Core <c>AuditInterceptor</c>
/// so entities remain unaware of auditing concerns. This base class is
/// identical across all five APIs — the audit shape does not change.
///
/// In API 5, both the new <see cref="AggregateRoot"/> base class and child
/// entities (Column, Note, ProjectMember) inherit from this base class.
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
