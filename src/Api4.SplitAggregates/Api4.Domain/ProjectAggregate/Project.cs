using Api4.Domain.Common;
using Api4.Domain.Exceptions;

namespace Api4.Domain.ProjectAggregate;

/// <summary>
/// Aggregate root for a project. Owns project membership.
/// All membership mutations go through this class to ensure invariants.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3. Project is a true aggregate root with an
/// optimistic concurrency token (<see cref="Version"/> mapped to PostgreSQL
/// <c>xmin</c>). The Project aggregate is unchanged between API 3 and API 4
/// because vote extraction does not affect project membership.
/// </remarks>
public class Project : AuditableEntityBase, IAggregateRoot
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

    /// <summary>
    /// Gets the concurrency token mapped to PostgreSQL <c>xmin</c> system column.
    /// </summary>
    public uint Version { get; private set; }

    /// <summary>Gets the read-only collection of members assigned to this project.</summary>
    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    /// <summary>
    /// Assigns a user to this project.
    /// </summary>
    /// <param name="userId">The ID of the user to add.</param>
    /// <returns>The created <see cref="ProjectMember"/> entity.</returns>
    /// <exception cref="InvariantViolationException">
    /// Thrown when the user is already a member of this project.
    /// </exception>
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

    /// <summary>
    /// Checks whether a user is a member of this project.
    /// </summary>
    /// <param name="userId">The ID of the user to check.</param>
    /// <returns><c>true</c> if the user is a member; otherwise <c>false</c>.</returns>
    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);
}
