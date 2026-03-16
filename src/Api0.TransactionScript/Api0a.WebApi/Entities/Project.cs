namespace Api0a.WebApi.Entities;

/// <summary>
/// Represents a project that can contain retro boards and members.
/// </summary>
/// <remarks>
/// DESIGN: Anemic entity — no aggregate boundary, no consistency guarantees.
/// In the Transaction Script pattern every entity is independently loadable
/// and saveable via the DbContext. There is no aggregate root concept.
/// </remarks>
public class Project : AuditableEntityBase
{
    /// <summary>Gets or sets the name of the project.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the collection of members assigned to this project.</summary>
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

    /// <summary>Gets or sets the collection of retro boards belonging to this project.</summary>
    public ICollection<RetroBoard> RetroBoards { get; set; } = new List<RetroBoard>();
}
