using Api2.Application.Interfaces;
using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api2.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProjectRepository"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, <see cref="GetByIdWithMembersAsync"/> is new — it loads
/// the project with its members collection so that <see cref="Project.AddMember"/>
/// and <see cref="Project.RemoveMember"/> can enforce invariants in-memory.
/// This is a hidden coupling between loading strategy and domain logic:
/// if you forget to use the WithMembers variant, the domain check works on
/// an empty collection and silently allows duplicates.
/// API 3 resolves this by always loading aggregate roots with their full graph.
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
        => await _context.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<Project?> GetByIdWithMembersAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
        => await _context.Projects.AddAsync(project, cancellationToken);
}
