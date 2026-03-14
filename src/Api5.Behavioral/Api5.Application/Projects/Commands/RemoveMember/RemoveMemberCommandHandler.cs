using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Domain.Exceptions;
using Api5.Domain.ProjectAggregate;
using MediatR;

namespace Api5.Application.Projects.Commands.RemoveMember;

/// <summary>
/// Handles the <see cref="RemoveMemberCommand"/> by loading the Project
/// aggregate, removing the member, and persisting the change.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a WRITE operation that modifies the Project
/// aggregate. The domain event <c>MemberRemovedFromProjectEvent</c> is
/// raised by the aggregate and dispatched after save.
/// </remarks>
public class RemoveMemberCommandHandler : IRequestHandler<RemoveMemberCommand, Unit>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="RemoveMemberCommandHandler"/>.
    /// </summary>
    /// <param name="projectRepository">The project repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public RemoveMemberCommandHandler(IProjectRepository projectRepository, IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Removes a user from a project's membership.
    /// </summary>
    /// <param name="request">The remove member command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see cref="Unit.Value"/> on success.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown when the project or member is not found.
    /// </exception>
    public async Task<Unit> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        Project project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        try
        {
            project.RemoveMember(request.UserId);
        }
        catch (DomainException)
        {
            throw new NotFoundException("ProjectMember", request.UserId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
