namespace Api0b.WebApi.Entities;

/// <summary>
/// Represents a project that can contain retro boards and members.
/// </summary>
/// <remarks>
/// DESIGN: Same anemic entity as Api0a, with one addition: the
/// <see cref="Version"/> property is mapped to PostgreSQL's <c>xmin</c>
/// system column for optimistic concurrency detection. See
/// <see cref="User"/> for the rationale.
/// </remarks>
public class Project : AuditableEntityBase
{
    /// <summary>Gets or sets the name of the project.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection of members assigned to this project.</summary>
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

    /// <summary>Gets or sets the collection of retro boards belonging to this project.</summary>
    public ICollection<RetroBoard> RetroBoards { get; set; } = new List<RetroBoard>();

    /// <summary>
    /// Gets or sets the concurrency token mapped to PostgreSQL's <c>xmin</c> system column.
    /// Used by EF Core for optimistic concurrency detection.
    /// </summary>
    public uint Version { get; set; }
}
