namespace Api1.Domain.Entities;

/// <summary>
/// Represents a retrospective board belonging to a <see cref="Project"/>.
/// A retro board contains multiple <see cref="Column"/>s.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 this entity is independently loadable — there is no
/// aggregate root that coordinates access to the retro and its children.
/// This means concurrent writes to columns under the same retro board
/// have no transactional coordination. API 3 makes RetroBoard an aggregate
/// root that owns Columns and Notes.
/// </remarks>
public class RetroBoard : AuditableEntityBase
{
    /// <summary>Gets or sets the ID of the project this retro board belongs to.</summary>
    public Guid ProjectId { get; set; }

    /// <summary>Gets or sets the name of the retro board.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the navigation property to the owning project.</summary>
    public Project Project { get; set; } = null!;

    /// <summary>Gets or sets the collection of columns in this retro board.</summary>
    public ICollection<Column> Columns { get; set; } = new List<Column>();
}
