using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;
using Api2.Application.Exceptions;
using Api2.Application.Interfaces;
using Api2.Domain.Entities;
using Api2.Domain.Exceptions;

namespace Api2.Application.Services;

/// <summary>
/// Orchestrates project membership operations by loading the Project entity
/// with its members, invoking domain behaviour, and persisting changes.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 1's <c>ProjectMemberService</c> — the duplicate
/// membership check has moved into <see cref="Project.AddMember"/>. The service
/// no longer needs a dedicated <c>IProjectMemberRepository</c>; membership
/// changes flow through the Project entity and are detected by EF Core's
/// change tracker when the <see cref="Project.Members"/> collection changes.
///
/// The service still verifies that the project and user exist (application-level
/// concerns) and translates domain exceptions into application exceptions where
/// the HTTP contract requires different status codes (e.g., 404 for "not a member").
/// </remarks>
public class ProjectMemberService : IProjectMemberService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ProjectMemberService"/>.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public ProjectMemberService(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ProjectMemberResponse> AddMemberAsync(
        Guid projectId,
        AddMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Load project with members (domain method needs the collection)
        Project project = await _projectRepository.GetByIdWithMembersAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        // 2. Verify the user exists (application-level concern)
        _ = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // 3. Domain method enforces "no duplicate members" invariant
        //    Throws InvariantViolationException if already a member
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
        // Load project with members (domain method needs the collection)
        Project project = await _projectRepository.GetByIdWithMembersAsync(projectId, cancellationToken)
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
