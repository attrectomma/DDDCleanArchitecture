namespace Api0a.WebApi.Entities;

/// <summary>
/// Represents a retrospective board belonging to a <see cref="Project"/>.
/// A retro board contains multiple <see cref="Column"/>s.
/// </summary>
/// <remarks>
/// DESIGN: In the Transaction Script pattern there is no aggregate root —
/// RetroBoard is just another entity. Concurrent writes to columns under
/// the same retro board have no transactional coordination. Api0b adds
/// xmin-based concurrency tokens to fix this at the database level.
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
