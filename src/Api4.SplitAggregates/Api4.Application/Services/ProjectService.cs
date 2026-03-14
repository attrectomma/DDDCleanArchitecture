using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;
using Api4.Application.Exceptions;
using Api4.Application.Interfaces;
using Api4.Domain.Exceptions;
using Api4.Domain.ProjectAggregate;
using Api4.Domain.UserAggregate;

namespace Api4.Application.Services;

/// <summary>
/// Application service for project and project membership operations.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3. The Project aggregate is unchanged between
/// API 3 and API 4. Thin orchestrator: load aggregate → call method → save.
/// </remarks>
public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectService"/>.
    /// </summary>
    /// <param name="projectRepository">The project aggregate repository.</param>
    /// <param name="userRepository">The user repository (for existence checks).</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public ProjectService(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
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

    /// <inheritdoc />
    public async Task<ProjectMemberResponse> AddMemberAsync(
        Guid projectId,
        AddMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        Project project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        _ = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        ProjectMember member = project.AddMember(request.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProjectMemberResponse(member.Id, member.ProjectId, member.UserId);
    }

    /// <inheritdoc />
    public async Task RemoveMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Project project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        try
        {
            project.RemoveMember(userId);
        }
        catch (DomainException)
        {
            throw new NotFoundException("ProjectMember", userId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
