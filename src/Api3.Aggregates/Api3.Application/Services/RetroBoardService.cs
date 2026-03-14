using Api3.Application.DTOs.Requests;
using Api3.Application.DTOs.Responses;
using Api3.Application.Exceptions;
using Api3.Application.Interfaces;
using Api3.Domain.Exceptions;
using Api3.Domain.ProjectAggregate;
using Api3.Domain.RetroAggregate;

namespace Api3.Application.Services;

/// <summary>
/// Application service for all retro board operations including columns,
/// notes, and votes. Replaces ColumnService, NoteService, and VoteService
/// from API 1/2.
/// </summary>
/// <remarks>
/// DESIGN: This service is a thin orchestrator:
///   1. Load aggregate
///   2. Call aggregate method (which enforces invariants)
///   3. Save via UoW
/// All business logic lives in the aggregate root. The service does not
/// contain any domain logic — it merely coordinates the load-modify-save cycle.
///
/// DESIGN (CQRS foreshadowing): Notice that GET operations also load the
/// full aggregate via the repository, even though they only need a read-only
/// view. This means read-heavy traffic pays the same cost as writes. API 5
/// addresses this with CQRS — queries bypass the aggregate and project
/// directly from the database.
/// </remarks>
public class RetroBoardService : IRetroBoardService
{
    private readonly IRetroBoardRepository _retroBoardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardService"/>.
    /// </summary>
    /// <param name="retroBoardRepository">The retro board aggregate repository.</param>
    /// <param name="projectRepository">The project repository (for existence checks).</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public RetroBoardService(
        IRetroBoardRepository retroBoardRepository,
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _retroBoardRepository = retroBoardRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<RetroBoardResponse> CreateAsync(
        Guid projectId,
        CreateRetroBoardRequest request,
        CancellationToken cancellationToken = default)
    {
        // Verify the project exists
        _ = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        var retroBoard = new RetroBoard(projectId, request.Name);

        await _retroBoardRepository.AddAsync(retroBoard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RetroBoardResponse(retroBoard.Id, retroBoard.Name, retroBoard.ProjectId, retroBoard.CreatedAt, null);
    }

    /// <inheritdoc />
    public async Task<RetroBoardResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RetroBoard retroBoard = await _retroBoardRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", id);

        List<ColumnResponse> columns = retroBoard.Columns
            .Select(c => new ColumnResponse(
                c.Id,
                c.Name,
                c.Notes.Select(n => new NoteResponse(
                    n.Id,
                    n.Text,
                    n.Votes.Count
                )).ToList()
            )).ToList();

        return new RetroBoardResponse(retroBoard.Id, retroBoard.Name, retroBoard.ProjectId, retroBoard.CreatedAt, columns);
    }

    // ── Column operations ───────────────────────────────────────

    /// <inheritdoc />
    public async Task<ColumnResponse> AddColumnAsync(
        Guid retroBoardId,
        CreateColumnRequest request,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // All invariant checking happens inside the aggregate
        Column column = retro.AddColumn(request.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ColumnResponse(column.Id, column.Name, null);
    }

    /// <inheritdoc />
    public async Task<ColumnResponse> RenameColumnAsync(
        Guid retroBoardId,
        Guid columnId,
        UpdateColumnRequest request,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // Aggregate root checks name uniqueness across all columns
        retro.RenameColumn(columnId, request.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Column column = retro.Columns.First(c => c.Id == columnId);
        return new ColumnResponse(column.Id, column.Name, null);
    }

    /// <inheritdoc />
    public async Task RemoveColumnAsync(
        Guid retroBoardId,
        Guid columnId,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        retro.RemoveColumn(columnId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ── Note operations ─────────────────────────────────────────

    /// <inheritdoc />
    public async Task<NoteResponse> AddNoteAsync(
        Guid retroBoardId,
        Guid columnId,
        CreateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        Note note = retro.AddNote(columnId, request.Text);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new NoteResponse(note.Id, note.Text, 0);
    }

    /// <inheritdoc />
    public async Task<NoteResponse> UpdateNoteAsync(
        Guid retroBoardId,
        Guid columnId,
        Guid noteId,
        UpdateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // Aggregate root delegates to Column, which checks text uniqueness
        retro.UpdateNote(columnId, noteId, request.Text);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Column column = retro.Columns.First(c => c.Id == columnId);
        Note note = column.Notes.First(n => n.Id == noteId);
        return new NoteResponse(note.Id, note.Text, null);
    }

    /// <inheritdoc />
    public async Task RemoveNoteAsync(
        Guid retroBoardId,
        Guid columnId,
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        retro.RemoveNote(columnId, noteId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ── Vote operations ─────────────────────────────────────────

    /// <inheritdoc />
    public async Task<VoteResponse> CastVoteAsync(
        Guid retroBoardId,
        Guid columnId,
        Guid noteId,
        CastVoteRequest request,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // DESIGN: Cross-aggregate membership check. The RetroBoard aggregate
        // knows its ProjectId but not its members — that belongs to the Project
        // aggregate. The application service coordinates the two aggregates to
        // enforce the "only project members may vote" invariant. This is a
        // cross-aggregate rule that cannot live in either aggregate alone.
        await EnsureUserIsProjectMemberAsync(retro.ProjectId, request.UserId, cancellationToken);

        Vote vote = retro.CastVote(columnId, noteId, request.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }

    /// <inheritdoc />
    public async Task RemoveVoteAsync(
        Guid retroBoardId,
        Guid columnId,
        Guid noteId,
        Guid voteId,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        retro.RemoveVote(columnId, noteId, voteId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ── Convenience methods for external REST contract ──────────
    // These methods look up the aggregate by child entity ID,
    // bridging the gap between the URL structure and the aggregate model.

    /// <inheritdoc />
    public async Task<NoteResponse> AddNoteByColumnIdAsync(
        Guid columnId,
        CreateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByColumnIdAsync(columnId, cancellationToken)
            ?? throw new NotFoundException("Column", columnId);

        Note note = retro.AddNote(columnId, request.Text);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new NoteResponse(note.Id, note.Text, 0);
    }

    /// <inheritdoc />
    public async Task<NoteResponse> UpdateNoteByColumnIdAsync(
        Guid columnId,
        Guid noteId,
        UpdateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByColumnIdAsync(columnId, cancellationToken)
            ?? throw new NotFoundException("Column", columnId);

        retro.UpdateNote(columnId, noteId, request.Text);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Column column = retro.Columns.First(c => c.Id == columnId);
        Note note = column.Notes.First(n => n.Id == noteId);
        return new NoteResponse(note.Id, note.Text, null);
    }

    /// <inheritdoc />
    public async Task RemoveNoteByColumnIdAsync(
        Guid columnId,
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByColumnIdAsync(columnId, cancellationToken)
            ?? throw new NotFoundException("Column", columnId);

        retro.RemoveNote(columnId, noteId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<VoteResponse> CastVoteByNoteIdAsync(
        Guid noteId,
        CastVoteRequest request,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByNoteIdAsync(noteId, cancellationToken)
            ?? throw new NotFoundException("Note", noteId);

        // DESIGN: Cross-aggregate membership check. The application service
        // coordinates the RetroBoard and Project aggregates to enforce the
        // "only project members may vote" invariant. This is the key benefit
        // of API 3's aggregate design — the service knows which aggregate
        // boundaries exist and orchestrates cross-boundary validation.
        await EnsureUserIsProjectMemberAsync(retro.ProjectId, request.UserId, cancellationToken);

        // Find the column containing this note
        Column column = retro.Columns.First(c => c.Notes.Any(n => n.Id == noteId));
        Vote vote = retro.CastVote(column.Id, noteId, request.UserId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }

    /// <inheritdoc />
    public async Task RemoveVoteByNoteIdAsync(
        Guid noteId,
        Guid voteId,
        CancellationToken cancellationToken = default)
    {
        RetroBoard retro = await _retroBoardRepository.GetByNoteIdAsync(noteId, cancellationToken)
            ?? throw new NotFoundException("Note", noteId);

        // Find the column containing this note
        Column column = retro.Columns.First(c => c.Notes.Any(n => n.Id == noteId));
        retro.RemoveVote(column.Id, noteId, voteId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────

    /// <summary>
    /// Verifies that the specified user is a member of the specified project.
    /// Throws <see cref="InvariantViolationException"/> if the project has members
    /// and the user is not one of them.
    /// </summary>
    /// <param name="projectId">The project's unique identifier.</param>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="NotFoundException">Thrown when the project is not found.</exception>
    /// <exception cref="InvariantViolationException">
    /// Thrown when the project has members and the user is not among them.
    /// </exception>
    /// <remarks>
    /// DESIGN: This is a cross-aggregate invariant check. The Project aggregate
    /// owns membership data, but the RetroBoard aggregate needs to verify
    /// membership before allowing vote operations. The application service is
    /// the natural place for this coordination — it has access to both
    /// aggregate repositories and can enforce the rule without either aggregate
    /// needing to know about the other.
    ///
    /// The check only applies when the project has at least one member assigned.
    /// If the project has no members, voting is unrestricted — this mirrors a
    /// "pre-membership" phase where the project is still being set up.
    ///
    /// In API 1/2, this check was simply missing — VoteService only verified
    /// that the note and user existed, not that the user was a project member.
    /// This is the hallmark problem of missing aggregate boundaries.
    /// </remarks>
    private async Task EnsureUserIsProjectMemberAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        Project project = await _projectRepository.GetByIdAsync(projectId, cancellationToken)
            ?? throw new NotFoundException("Project", projectId);

        // DESIGN: Only enforce membership when the project has members assigned.
        // A project with no members is in an "open" state where any user can
        // participate. Once at least one member is assigned, only members may vote.
        if (project.Members.Count > 0 && !project.IsMember(userId))
            throw new InvariantViolationException(
                $"User {userId} is not a member of project {projectId} and cannot vote.");
    }
}
