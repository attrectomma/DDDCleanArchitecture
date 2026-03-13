using Api1.Application.Interfaces;
using Api1.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api1.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProjectMemberRepository"/>.
/// </summary>
public class ProjectMemberRepository : IProjectMemberRepository
{
    private readonly RetroBoardDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectMemberRepository"/>.
    /// </summary>
    /// <param name="context">The EF Core DbContext.</param>
    public ProjectMemberRepository(RetroBoardDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        => await _context.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(ProjectMember member, CancellationToken cancellationToken = default)
        => await _context.ProjectMembers.AddAsync(member, cancellationToken);

    /// <inheritdoc />
    public async Task<ProjectMember?> GetByProjectAndUserAsync(
        Guid projectId, Guid userId, CancellationToken cancellationToken = default)
        => await _context.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public void Delete(ProjectMember member)
        => _context.ProjectMembers.Remove(member);
}
