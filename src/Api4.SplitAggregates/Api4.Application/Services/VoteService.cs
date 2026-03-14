using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;
using Api4.Application.Exceptions;
using Api4.Application.Interfaces;
using Api4.Domain.Exceptions;
using Api4.Domain.ProjectAggregate;
using Api4.Domain.RetroAggregate;
using Api4.Domain.VoteAggregate;

namespace Api4.Application.Services;

/// <summary>
/// Application service for vote operations. Coordinates across
/// the Vote, RetroBoard, and Project aggregates.
/// </summary>
/// <remarks>
/// DESIGN: This is the key difference from API 3. Voting now requires:
///   1. Verify the note exists (query RetroBoard or Note table).
///   2. Verify the user is a project member (query Project aggregate).
///   3. Check for existing vote (query Vote aggregate or rely on DB constraint).
///   4. Create and persist the Vote aggregate.
///
/// Cross-aggregate invariants:
///   - "Note must exist" → checked via a lightweight read query (not
///     transactionally safe if the note is deleted concurrently, but
///     acceptable — the FK constraint on the Vote table would catch it).
///   - "User is project member" → checked via Project aggregate read.
///   - "One vote per user per note" → checked in app + DB unique constraint.
///
/// IMPORTANT: Steps 1-3 are READ operations on OTHER aggregates. They are
/// NOT in the same transaction as step 4. This is the cost of splitting
/// aggregates — we lose transactional consistency across them.
/// </remarks>
public class VoteService : IVoteService
{
    private readonly IVoteRepository _voteRepository;
    private readonly IRetroBoardRepository _retroRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="VoteService"/>.
    /// </summary>
    /// <param name="voteRepository">The vote aggregate repository.</param>
    /// <param name="retroRepository">The retro board repository (for note existence checks).</param>
    /// <param name="projectRepository">The project repository (for membership checks).</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public VoteService(
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

    /// <inheritdoc />
    public async Task<VoteResponse> CastVoteAsync(
        Guid noteId,
        CastVoteRequest request,
        CancellationToken cancellationToken = default)
    {
        // Cross-aggregate check 1: Does the note exist?
        // DESIGN: We use a lightweight query instead of loading the full
        // RetroBoard aggregate just to check note existence. This is more
        // efficient and avoids unnecessary write locks on the retro board.
        bool noteExists = await _retroRepository.NoteExistsAsync(noteId, cancellationToken);
        if (!noteExists)
            throw new NotFoundException("Note", noteId);

        // Cross-aggregate check 2: Is the user a project member?
        // DESIGN: We load the RetroBoard aggregate to get its ProjectId,
        // then check membership via the Project aggregate. This is a
        // cross-aggregate orchestration that only the application service can do.
        RetroBoard? retro = await _retroRepository.GetByNoteIdAsync(noteId, cancellationToken);
        if (retro is not null)
        {
            await EnsureUserIsProjectMemberAsync(retro.ProjectId, request.UserId, cancellationToken);
        }

        // Cross-aggregate check 3: Already voted?
        // DESIGN: This is a "best effort" check. Under high concurrency,
        // two requests could both pass this check. The DB unique constraint
        // on (NoteId, UserId) is the real safety net.
        if (await _voteRepository.ExistsAsync(noteId, request.UserId, cancellationToken))
            throw new InvariantViolationException(
                $"User {request.UserId} has already voted on note {noteId}.");

        var vote = new Vote(noteId, request.UserId);
        await _voteRepository.AddAsync(vote, cancellationToken);

        // DESIGN: If the DB unique constraint catches a race condition,
        // the GlobalExceptionHandlerMiddleware will convert the
        // DbUpdateException (PostgreSQL error 23505) to a 409 Conflict.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }

    /// <inheritdoc />
    public async Task RemoveVoteAsync(
        Guid noteId,
        Guid voteId,
        CancellationToken cancellationToken = default)
    {
        Vote vote = await _voteRepository.GetByIdAsync(voteId, cancellationToken)
            ?? throw new NotFoundException("Vote", voteId);

        _voteRepository.Delete(vote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────

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
    /// <remarks>
    /// DESIGN: Same cross-aggregate membership check as API 3's RetroBoardService.
    /// The difference is that in API 3 this was part of the RetroBoardService
    /// (which handled votes), and now it lives in the VoteService.
    /// The check only applies when the project has at least one member assigned.
    /// </remarks>
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
