using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;
using Api1.Application.Exceptions;
using Api1.Application.Interfaces;
using Api1.Domain.Entities;

namespace Api1.Application.Services;

/// <summary>
/// Service responsible for retro board business logic.
/// </summary>
public class RetroBoardService : IRetroBoardService
{
    private readonly IRetroBoardRepository _retroBoardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardService"/>.
    /// </summary>
    /// <param name="retroBoardRepository">The retro board repository.</param>
    /// <param name="projectRepository">The project repository.</param>
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

        var retroBoard = new RetroBoard
        {
            ProjectId = projectId,
            Name = request.Name
        };

        await _retroBoardRepository.AddAsync(retroBoard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RetroBoardResponse(retroBoard.Id, retroBoard.Name, retroBoard.ProjectId, retroBoard.CreatedAt, null);
    }

    /// <inheritdoc />
    public async Task<RetroBoardResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        RetroBoard retroBoard = await _retroBoardRepository.GetByIdWithDetailsAsync(id, cancellationToken)
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
}
