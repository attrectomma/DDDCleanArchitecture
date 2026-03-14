namespace Api3.Domain.ProjectAggregate;

/// <summary>
/// Repository contract for the <see cref="Project"/> aggregate root.
/// Always loads the complete aggregate (project + members).
/// </summary>
/// <remarks>
/// DESIGN: In API 3, repository interfaces live in the Domain layer.
/// The Project repository always loads the full aggregate graph (including
/// members) so the aggregate root can enforce invariants. There is no
/// <c>GetByIdWithMembersAsync</c> vs. <c>GetByIdAsync</c> distinction
/// like in API 2 — the aggregate is always loaded completely. This
/// eliminates the hidden coupling between loading strategy and domain logic.
/// </remarks>
public interface IProjectRepository
{
    /// <summary>
    /// Retrieves a project by ID with all its members loaded.
    /// </summary>
    /// <param name="id">The project's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The project with members, or <c>null</c> if not found.</returns>
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new project to the repository.</summary>
    /// <param name="project">The project to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
}
