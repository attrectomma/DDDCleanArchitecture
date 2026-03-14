using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.Exceptions;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.RetroAggregate;
using Api5.Domain.VoteAggregate;
using MediatR;

namespace Api5.Application.Votes.Commands.CastVote;

/// <summary>
/// Handles the <see cref="CastVoteCommand"/> by performing cross-aggregate
/// validation, creating a Vote aggregate, and persisting it.
/// </summary>
/// <remarks>
/// DESIGN: Each handler is a focused unit of work for a single use case.
/// Compare with API 4's VoteService which handled both CastVote and
/// RemoveVote. Here, each operation has its own handler, making it
/// easier to:
///   - Test in isolation
///   - Add cross-cutting concerns via pipeline behaviors
///   - Reason about a single code path
///
/// The handler's logic is identical to API 4's VoteService.CastVoteAsync,
/// but the structure is fundamentally different.
///
/// The Vote constructor raises a <see cref="Api5.Domain.VoteAggregate.Events.VoteCastEvent"/>
/// which is dispatched after save by the <c>DomainEventInterceptor</c>.
/// </remarks>
public class CastVoteCommandHandler : IRequestHandler<CastVoteCommand, VoteResponse>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IRetroBoardRepository _retroRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="CastVoteCommandHandler"/>.
    /// </summary>
    /// <param name="voteRepository">The vote aggregate repository.</param>
    /// <param name="retroRepository">The retro board repository (for note existence checks).</param>
    /// <param name="projectRepository">The project repository (for membership checks).</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public CastVoteCommandHandler(
        IVoteRepository voteRepository,
        IRetroBoardRepository retroRepository,
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _voteRepository = voteRepository;
        _retroRepository = retroRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Casts a vote on a note after cross-aggregate validation.
    /// </summary>
    /// <param name="request">The cast vote command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created vote response.</returns>
    /// <exception cref="NotFoundException">Thrown when the note or project is not found.</exception>
    /// <exception cref="InvariantViolationException">
    /// Thrown when the user already voted on the note or is not a project member.
    /// </exception>
    public async Task<VoteResponse> Handle(CastVoteCommand request, CancellationToken cancellationToken)
    {
        // Cross-aggregate check 1: Does the note exist?
        bool noteExists = await _retroRepository.NoteExistsAsync(request.NoteId, cancellationToken);
        if (!noteExists)
            throw new NotFoundException("Note", request.NoteId);

        // Cross-aggregate check 2: Is the user a project member?
        RetroBoard? retro = await _retroRepository.GetByNoteIdAsync(request.NoteId, cancellationToken);
        if (retro is not null)
        {
            await EnsureUserIsProjectMemberAsync(retro.ProjectId, request.UserId, cancellationToken);
        }

        // Cross-aggregate check 3: Already voted?
        if (await _voteRepository.ExistsAsync(request.NoteId, request.UserId, cancellationToken))
            throw new InvariantViolationException(
                $"User {request.UserId} has already voted on note {request.NoteId}.");

        // DESIGN: The Vote constructor raises a VoteCastEvent domain event.
        var vote = new Vote(request.NoteId, request.UserId);
        await _voteRepository.AddAsync(vote, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }

    /// <summary>
    /// Verifies that the specified user is a member of the specified project.
    /// </summary>
    /// <param name="projectId">The project's unique identifier.</param>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="NotFoundException">Thrown when the project is not found.</exception>
    /// <exception cref="InvariantViolationException">
    /// Thrown when the project has members and the user is not among them.
    /// </exception>
    private async Task EnsureUserIsProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Project project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        if (project.Members.Count > 0 && !project.IsMember(userId))
            throw new InvariantViolationException(
                $"User {userId} is not a member of project {projectId} and cannot vote.");
    }
}
