using Api2.Domain.Entities;

namespace Api2.Application.Interfaces;

/// <summary>
/// Repository contract for <see cref="Project"/> entities.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, the Project entity now manages its own membership via
/// <see cref="Project.AddMember"/> and <see cref="Project.RemoveMember"/>.
/// This repository adds <see cref="GetByIdWithMembersAsync"/> to load the
/// project with its members collection, which the domain methods require.
/// The separate <c>IProjectMemberRepository</c> from API 1 is no longer needed.
/// </remarks>
public interface IProjectRepository
{
    /// <summary>Retrieves a project by its unique identifier.</summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The project entity, or <c>null</c> if not found.</returns>
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a project by its ID, eagerly loading its <see cref="Project.Members"/> collection.
    /// Required for <see cref="Project.AddMember"/> and <see cref="Project.RemoveMember"/>
    /// domain methods to enforce invariants.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The project entity with its members, or <c>null</c> if not found.</returns>
    Task<Project?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Adds a new project to the repository.</summary>
    /// <param name="project">The project entity to add.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
}
