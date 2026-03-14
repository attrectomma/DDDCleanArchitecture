using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api5.Application.Projects.Queries.GetProject;

/// <summary>
/// Handles the <see cref="GetProjectQuery"/> by projecting directly from
/// the database.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): Query handlers depend on <see cref="IReadOnlyDbContext"/>
/// directly, NOT on <c>IProjectRepository</c>. The repository's job is to
/// load and persist AGGREGATES — it's a write-side concept.
/// </remarks>
public class GetProjectQueryHandler : IRequestHandler<GetProjectQuery, ProjectResponse>
{
    private readonly IReadOnlyDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="GetProjectQueryHandler"/>.
    /// </summary>
    /// <param name="context">The read-only database context.</param>
    public GetProjectQueryHandler(IReadOnlyDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Projects a project directly from the database as a response DTO.
    /// </summary>
    /// <param name="request">The get project query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The project response.</returns>
    /// <exception cref="NotFoundException">Thrown when the project is not found.</exception>
    public async Task<ProjectResponse> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        ProjectResponse? result = await _context.Projects
            .AsNoTracking()
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new ProjectResponse(p.Id, p.Name, p.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return result ?? throw new NotFoundException("Project", request.ProjectId);
    }
}
