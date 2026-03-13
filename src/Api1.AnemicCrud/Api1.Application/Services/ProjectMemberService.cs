using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;
using Api1.Application.Exceptions;
using Api1.Application.Interfaces;
using Api1.Domain.Entities;

namespace Api1.Application.Services;

/// <summary>
/// Service responsible for project membership business logic.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 the ProjectMember join entity gets its own dedicated
/// service (and repository). This 1-to-1 mapping is a hallmark of the anemic
/// CRUD approach. API 3+ manage membership through the Project aggregate,
/// eliminating the need for a separate service and repository.
/// </remarks>
public class ProjectMemberService : IProjectMemberService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IProjectMemberRepository _projectMemberRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectMemberService"/>.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="projectMemberRepository">The project member repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public ProjectMemberService(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IProjectMemberRepository projectMemberRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _projectMemberRepository = projectMemberRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ProjectMemberResponse> AddMemberAsync(
        Guid projectId,
        AddMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify the project exists
        _ = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        // 2. Verify the user exists
        _ = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // 3. Check for duplicate membership
        if (await _projectMemberRepository.ExistsAsync(projectId, request.UserId, cancellationToken))
            throw new DuplicateException("ProjectMember", "UserId", request.UserId.ToString());

        // 4. Create the membership
        var member = new ProjectMember
        {
            ProjectId = projectId,
            UserId = request.UserId
        };

        await _projectMemberRepository.AddAsync(member, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProjectMemberResponse(member.Id, member.ProjectId, member.UserId);
    }

    /// <inheritdoc />
    public async Task RemoveMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ProjectMember member = await _projectMemberRepository.GetByProjectAndUserAsync(projectId, userId, cancellationToken)
            ?? throw new NotFoundException("ProjectMember", userId);

        _projectMemberRepository.Delete(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
