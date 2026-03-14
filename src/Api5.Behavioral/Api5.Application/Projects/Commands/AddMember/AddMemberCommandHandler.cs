using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.UserAggregate;
using MediatR;

namespace Api5.Application.Projects.Commands.AddMember;

/// <summary>
/// Handles the <see cref="AddMemberCommand"/> by loading the Project aggregate,
/// adding the member, and persisting the change.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a WRITE operation. We MUST load the aggregate
/// through the repository so the aggregate root can enforce invariants
/// (e.g., no duplicate members). The aggregate is the write model.
///
/// Compare with API 4's <c>ProjectService.AddMemberAsync</c> — the logic
/// is identical, but the structure is fundamentally different. Each use
/// case has its own handler class.
/// </remarks>
public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand, ProjectMemberResponse>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="AddMemberCommandHandler"/>.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="userRepository">The user repository (for existence checks).</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public AddMemberCommandHandler(
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Adds a user as a member to a project aggregate.
    /// </summary>
    /// <param name="request">The add member command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created membership response.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown when the project or user is not found.
    /// </exception>
    public async Task<ProjectMemberResponse> Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        Project project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        _ = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        ProjectMember member = project.AddMember(request.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProjectMemberResponse(member.Id, member.ProjectId, member.UserId);
    }
}
