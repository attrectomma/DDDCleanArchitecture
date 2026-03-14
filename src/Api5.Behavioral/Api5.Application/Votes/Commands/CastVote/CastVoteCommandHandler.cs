using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.RetroAggregate;
using Api5.Domain.VoteAggregate;
using Api5.Domain.VoteAggregate.Strategies;
using MediatR;

namespace Api5.Application.Votes.Commands.CastVote;

/// <summary>
/// Handles the <see cref="CastVoteCommand"/> by building a
/// <see cref="VoteEligibilityContext"/>, resolving the board's
/// <see cref="IVotingStrategy"/>, validating via composed specifications,
/// and persisting the vote.
/// </summary>
/// <remarks>
/// DESIGN: This handler demonstrates the Strategy + Specification patterns
/// working together. Compare with the API 4 version where all voting rules
/// were inline checks:
/// <code>
///   if (await _voteRepo.ExistsAsync(noteId, userId, ct))
///       throw new InvariantViolationException(...);
/// </code>
///
/// In API 5, the handler's responsibilities are:
///   1. Gather data from repositories to build the <see cref="VoteEligibilityContext"/>.
///   2. Resolve the correct <see cref="IVotingStrategy"/> from the board's configuration.
///   3. Delegate validation to the strategy (which composes domain specifications).
///   4. Create and persist the Vote aggregate.
///
/// This separation means adding a new voting strategy (e.g., "Ranked Choice")
/// requires zero changes to this handler — only a new strategy class and enum value.
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
    /// <param name="retroRepository">The retro board repository (for note/column lookup).</param>
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
    /// Casts a vote on a note after strategy-based validation.
    /// </summary>
    /// <param name="request">The cast vote command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created vote response.</returns>
    /// <exception cref="NotFoundException">Thrown when the note or project is not found.</exception>
    /// <exception cref="Api5.Domain.Exceptions.InvariantViolationException">
    /// Thrown when a voting rule is violated (duplicate vote, budget exceeded, non-member).
    /// </exception>
    public async Task<VoteResponse> Handle(CastVoteCommand request, CancellationToken cancellationToken)
    {
        // Step 1: Load the retro board aggregate to find the note, column, and voting strategy.
        RetroBoard retro = await _retroRepository.GetByNoteIdAsync(request.NoteId, cancellationToken)
            ?? throw new NotFoundException("Note", request.NoteId);

        Column? column = retro.Columns.FirstOrDefault(c => c.Notes.Any(n => n.Id == request.NoteId));
        if (column is null)
            throw new NotFoundException("Note", request.NoteId);

        // Step 2: Gather data for the eligibility context.
        bool userIsProjectMember = await CheckUserIsProjectMemberAsync(
            retro.ProjectId, request.UserId, cancellationToken);

        bool userAlreadyVoted = await _voteRepository.ExistsAsync(
            request.NoteId, request.UserId, cancellationToken);

        int userVoteCountInColumn = await _voteRepository.CountByColumnAndUserAsync(
            column.Id, request.UserId, cancellationToken);

        // Step 3: Build the immutable eligibility context.
        // DESIGN: The context separates data gathering (here, async + repos)
        // from rule evaluation (in the strategy, synchronous + pure).
        var eligibilityContext = new VoteEligibilityContext(
            NoteId: request.NoteId,
            UserId: request.UserId,
            ColumnId: column.Id,
            RetroBoardId: retro.Id,
            ProjectId: retro.ProjectId,
            NoteExists: true,
            UserIsProjectMember: userIsProjectMember,
            UserAlreadyVotedOnNote: userAlreadyVoted,
            UserVoteCountInColumn: userVoteCountInColumn);

        // Step 4: Resolve the strategy and validate.
        // DESIGN: The factory maps the board's VotingStrategyType enum to a
        // concrete IVotingStrategy. The strategy composes specifications via
        // AND and throws targeted domain exceptions on failure.
        IVotingStrategy strategy = VotingStrategyFactory.Create(retro.VotingStrategyType);
        strategy.Validate(eligibilityContext);

        // Step 5: Create the vote aggregate and persist.
        // DESIGN: The Vote constructor raises a VoteCastEvent domain event.
        var vote = new Vote(request.NoteId, request.UserId);
        await _voteRepository.AddAsync(vote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }

    /// <summary>
    /// Checks whether the user is a member of the specified project.
    /// Returns <c>true</c> if the project has no members (open access)
    /// or if the user is explicitly a member.
    /// </summary>
    /// <param name="projectId">The project's unique identifier.</param>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the user is eligible to vote; otherwise <c>false</c>.</returns>
    /// <exception cref="NotFoundException">Thrown when the project is not found.</exception>
    private async Task<bool> CheckUserIsProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Project project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        return project.Members.Count == 0 || project.IsMember(userId);
    }
}
