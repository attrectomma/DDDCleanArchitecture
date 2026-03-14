namespace Api5.Domain.ProjectAggregate;

/// <summary>
/// Repository contract for the <see cref="Project"/> aggregate root.
/// Always loads the complete aggregate (project + members).
/// </summary>
/// <remarks>
/// DESIGN: Same as API 3/4. In API 5, this repository is used only by
/// command handlers (write side). The <c>GetProjectQueryHandler</c>
/// (read side) bypasses the repository and queries <c>DbContext</c>
/// directly with no-tracking projections — the CQRS split.
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
