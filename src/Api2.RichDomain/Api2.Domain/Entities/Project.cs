using Api2.Domain.Exceptions;

namespace Api2.Domain.Entities;

/// <summary>
/// A project that groups users and retro boards together.
/// </summary>
/// <remarks>
/// DESIGN: Unlike API 1 where Project was a plain DTO and membership was
/// managed by a dedicated <c>ProjectMemberService</c> + <c>ProjectMemberRepository</c>,
/// API 2's Project entity owns its membership logic via <see cref="AddMember"/>
/// and <see cref="RemoveMember"/>. The service becomes a thin orchestrator
/// that loads the Project with its members, calls the domain method, and saves.
///
/// This eliminates the separate <c>IProjectMemberRepository</c> — membership
/// changes flow through the Project entity and are detected by EF Core's
/// change tracker. However, without aggregate boundaries (API 3+), there
/// is no concurrency protection — two simultaneous AddMember calls could
/// both pass the duplicate check.
/// </remarks>
public class Project : AuditableEntityBase
{
    private readonly List<ProjectMember> _members = new();

    /// <summary>
    /// Required by EF Core for entity materialisation.
    /// </summary>
    private Project() { }

    /// <summary>
    /// Creates a new project with the specified name.
    /// </summary>
    /// <param name="name">The name of the project.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is null, empty, or whitespace.
    /// </exception>
    public Project(string name)
    {
        Name = Guard.AgainstNullOrWhiteSpace(name, nameof(name));
    }

    /// <summary>Gets the name of the project.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the read-only collection of members assigned to this project.</summary>
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    /// <summary>Gets the collection of retro boards belonging to this project.</summary>
    public ICollection<RetroBoard> RetroBoards { get; private set; } = new List<RetroBoard>();

    /// <summary>
    /// Assigns a user to this project.
    /// </summary>
    /// <param name="userId">The ID of the user to add.</param>
    /// <returns>The created <see cref="ProjectMember"/> entity.</returns>
    /// <exception cref="InvariantViolationException">
    /// Thrown when the user is already a member of this project.
    /// </exception>
    /// <remarks>
    /// DESIGN: In API 1, this check was done by <c>ProjectMemberService</c>
    /// via a repository query (<c>ExistsAsync</c>). Now the domain entity
    /// enforces the invariant directly. However, the Project must be loaded
    /// with its <see cref="Members"/> collection for this check to work —
    /// a hidden coupling between loading strategy and domain logic.
    /// API 3 resolves this by making Project an aggregate root that is
    /// always loaded with its members.
    /// </remarks>
    public ProjectMember AddMember(Guid userId)
    {
        if (_members.Any(m => m.UserId == userId))
            throw new InvariantViolationException($"User {userId} is already a member of this project.");

        var member = new ProjectMember(Id, userId);
        _members.Add(member);
        return member;
    }

    /// <summary>
    /// Removes a user from this project.
    /// </summary>
    /// <param name="userId">The ID of the user to remove.</param>
    /// <exception cref="DomainException">
    /// Thrown when the user is not a member of this project.
    /// </exception>
    public void RemoveMember(Guid userId)
    {
        ProjectMember member = _members.FirstOrDefault(m => m.UserId == userId)
            ?? throw new DomainException($"User {userId} is not a member of this project.");

        _members.Remove(member);
    }
}
