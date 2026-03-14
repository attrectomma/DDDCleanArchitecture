using Api3.Domain.ProjectAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api3.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProjectRepository"/>.
/// Always loads the complete Project aggregate (project + members).
/// </summary>
/// <remarks>
/// DESIGN: Unlike API 2, there is no <c>GetByIdWithMembersAsync</c> vs.
/// <c>GetByIdAsync</c> split. The aggregate repository always loads the
/// full graph. This eliminates the hidden coupling between loading strategy
/// and domain logic — you cannot accidentally load a Project without its
/// members and then call <see cref="Project.AddMember"/> on an empty list.
/// </remarks>
public class ProjectRepository : IProjectRepository
{
    private readonly RetroBoardDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public ProjectRepository(RetroBoardDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
        => await _context.Projects.AddAsync(project, cancellationToken);
}
