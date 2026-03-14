using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;
using Api2.Application.Exceptions;
using Api2.Application.Interfaces;
using Api2.Domain.Entities;

namespace Api2.Application.Services;

/// <summary>
/// Service responsible for project-related operations.
/// </summary>
/// <remarks>
/// DESIGN: Same thin orchestration as API 1 — the only change is that
/// the Project entity is created via a factory constructor instead of
/// object-initializer syntax.
/// </remarks>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectService"/>.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public ProjectService(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var project = new Project(request.Name);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProjectResponse(project.Id, project.Name, project.CreatedAt);
    }

    /// <inheritdoc />
    public async Task<ProjectResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Project project = await _projectRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Project", id);

        return new ProjectResponse(project.Id, project.Name, project.CreatedAt);
    }
}
