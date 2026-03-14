using Api3.Application.DTOs.Requests;
using Api3.Application.DTOs.Responses;
using Api3.Application.Exceptions;
using Api3.Application.Interfaces;
using Api3.Domain.Exceptions;
using Api3.Domain.ProjectAggregate;
using Api3.Domain.UserAggregate;

namespace Api3.Application.Services;

/// <summary>
/// Application service for project and project membership operations.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, project CRUD and membership were in separate services
/// (<c>ProjectService</c> and <c>ProjectMemberService</c>). In API 3,
/// a single service covers the entire Project aggregate because all
/// membership logic is inside the aggregate root.
///
/// The service is a thin orchestrator:
///   1. Load aggregate (always with full graph)
///   2. Call aggregate method (which enforces invariants)
///   3. Save via UoW
///
/// No more hidden coupling between loading strategy and domain logic —
/// the repository always loads the complete aggregate.
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
        // 1. Load the Project aggregate (always includes members)
        Project project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        // 2. Verify the user exists (application-level concern)
        _ = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // 3. Aggregate root enforces "no duplicate members" invariant
        //    Protected by xmin concurrency token against races
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
        // Load the Project aggregate (always includes members)
        Project project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        // DESIGN: The domain method throws DomainException if the user is not a member.
        // We translate this to NotFoundException for the HTTP 404 contract.
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
