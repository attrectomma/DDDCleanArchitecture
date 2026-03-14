using Api4.Application.DTOs.Requests;
using Api4.Application.DTOs.Responses;
using Api4.Application.Exceptions;
using Api4.Application.Interfaces;
using Api4.Domain.ProjectAggregate;
using Api4.Domain.RetroAggregate;
using Api4.Domain.VoteAggregate;

namespace Api4.Application.Services;

/// <summary>
/// Application service for retro board operations including columns and notes,
/// but NOT votes. Votes are handled by <see cref="VoteService"/>.
/// </summary>
/// <remarks>
/// DESIGN: Compared to API 3's RetroBoardService, this no longer has vote
/// operations (CastVoteAsync, RemoveVoteAsync, CastVoteByNoteIdAsync, etc.).
/// Those moved to <see cref="VoteService"/> because Vote is now a separate
/// aggregate.
///
/// For GET operations that return vote counts, this service depends on
/// <see cref="IVoteRepository"/> to query counts across the aggregate boundary.
/// This cross-aggregate read dependency is an explicit cost of splitting the
/// Vote aggregate out.
///
/// DESIGN (CQRS foreshadowing): GET operations still load the full aggregate
/// via the repository even though they only need a read-only view. API 5
/// addresses this with CQRS — queries bypass the aggregate entirely.
/// </remarks>
public class RetroBoardService : IRetroBoardService
{
    private readonly IRetroBoardRepository _retroBoardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardService"/>.
    /// </summary>
    /// <param name="retroBoardRepository">The retro board aggregate repository.</param>
    /// <param name="projectRepository">The project repository (for existence checks).</param>
    /// <param name="voteRepository">The vote repository (for vote counts in GET responses).</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public RetroBoardService(
        IRetroBoardRepository retroBoardRepository,
        IProjectRepository projectRepository,
        IVoteRepository voteRepository,
        IUnitOfWork unitOfWork)
    {
        _retroBoardRepository = retroBoardRepository;
        _projectRepository = projectRepository;
        _voteRepository = voteRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<RetroBoardResponse> CreateAsync(
        Guid projectId,
        CreateRetroBoardRequest request,
        CancellationToken cancellationToken = default)
    {
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

        // DESIGN: Since votes are a separate aggregate, we need a cross-aggregate
        // query to get vote counts. We gather all note IDs and batch-query the
        // vote counts from the Vote repository. This is an explicit cost of
        // splitting the Vote aggregate — in API 3, we had note.Votes.Count.
        List<Guid> noteIds = retroBoard.Columns
            .SelectMany(c => c.Notes)
            .Select(n => n.Id)
            .ToList();

        Dictionary<Guid, int> voteCounts = noteIds.Count > 0
            ? await _voteRepository.GetVoteCountsByNoteIdsAsync(noteIds, cancellationToken)
            : new Dictionary<Guid, int>();

        List<ColumnResponse> columns = retroBoard.Columns
            .Select(c => new ColumnResponse(
                c.Id,
                c.Name,
                c.Notes.Select(n => new NoteResponse(
                    n.Id,
                    n.Text,
                    voteCounts.GetValueOrDefault(n.Id, 0)
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

    // ── Note operations (by column ID) ──────────────────────────

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

    // ❌ NO vote operations — those moved to VoteService
}
