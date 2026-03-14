using Api4.Domain.ProjectAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api4.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProjectRepository"/>.
/// Always loads the complete Project aggregate (project + members).
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3. The Project aggregate is unchanged.
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
