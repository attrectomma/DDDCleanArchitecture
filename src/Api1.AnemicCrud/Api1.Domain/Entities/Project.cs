namespace Api1.Domain.Entities;

/// <summary>
/// Represents a project that can contain retro boards and members.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 there is no aggregate boundary — Project, RetroBoard,
/// Column, Note, and Vote are all independently loadable and saveable.
/// This means there is no single place to enforce cross-entity invariants.
/// API 3 introduces the concept of an aggregate root that owns its children.
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
