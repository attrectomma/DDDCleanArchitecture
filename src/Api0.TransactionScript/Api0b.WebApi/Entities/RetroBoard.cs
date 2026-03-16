namespace Api0b.WebApi.Entities;

/// <summary>
/// Represents a retrospective board belonging to a <see cref="Project"/>.
/// A retro board contains multiple <see cref="Column"/>s.
/// </summary>
/// <remarks>
/// DESIGN: Same anemic entity as Api0a, with one addition: the
/// <see cref="Version"/> property is mapped to PostgreSQL's <c>xmin</c>
/// system column for optimistic concurrency detection.
///
/// In the DDD path (API 3+), <c>xmin</c> is configured only on aggregate
/// roots because the aggregate is the consistency boundary. Here we apply
/// <c>xmin</c> to individual entities that are the "root" of a logical
/// group. The effect is the same — EF Core adds a
/// <c>WHERE xmin = @expected</c> clause to UPDATE statements — but the
/// reasoning is different. There is no "aggregate" concept; we're just
/// telling the database "don't let two writers clobber each other."
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

    /// <summary>
    /// Gets or sets the concurrency token mapped to PostgreSQL's <c>xmin</c> system column.
    /// Used by EF Core for optimistic concurrency detection.
    /// </summary>
    public uint Version { get; set; }
}
