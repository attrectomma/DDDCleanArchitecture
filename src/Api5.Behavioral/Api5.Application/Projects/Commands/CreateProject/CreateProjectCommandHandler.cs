using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.ProjectAggregate;
using MediatR;

namespace Api5.Application.Projects.Commands.CreateProject;

/// <summary>
/// Handles the <see cref="CreateProjectCommand"/> by creating a Project
/// aggregate and persisting it.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a WRITE operation. We use the repository to
/// persist the aggregate and the unit of work to commit.
/// </remarks>
public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectResponse>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="CreateProjectCommandHandler"/>.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public CreateProjectCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a new project aggregate and persists it.
    /// </summary>
    /// <param name="request">The create project command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created project response.</returns>
    public async Task<ProjectResponse> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = new Project(request.Name);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProjectResponse(project.Id, project.Name, project.CreatedAt);
    }
}
