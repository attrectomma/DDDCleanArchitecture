using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.RetroAggregate;
using MediatR;

namespace Api5.Application.Retros.Commands.CreateRetroBoard;

/// <summary>
/// Handles the <see cref="CreateRetroBoardCommand"/> by verifying the project
/// exists, creating a RetroBoard aggregate, and persisting it.
/// </summary>
public class CreateRetroBoardCommandHandler : IRequestHandler<CreateRetroBoardCommand, RetroBoardResponse>
{
    private readonly IRetroBoardRepository _retroBoardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="CreateRetroBoardCommandHandler"/>.
    /// </summary>
    /// <param name="retroBoardRepository">The retro board repository.</param>
    /// <param name="projectRepository">The project repository (for existence checks).</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public CreateRetroBoardCommandHandler(
        IRetroBoardRepository retroBoardRepository,
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _retroBoardRepository = retroBoardRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Creates a new retro board aggregate and persists it.
    /// </summary>
    /// <param name="request">The create retro board command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created retro board response.</returns>
    /// <exception cref="NotFoundException">Thrown when the project is not found.</exception>
    public async Task<RetroBoardResponse> Handle(CreateRetroBoardCommand request, CancellationToken cancellationToken)
    {
        _ = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new NotFoundException("Project", request.ProjectId);

        var retroBoard = new RetroBoard(request.ProjectId, request.Name, request.VotingStrategyType);

        await _retroBoardRepository.AddAsync(retroBoard, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RetroBoardResponse(
            retroBoard.Id,
            retroBoard.Name,
            retroBoard.ProjectId,
            retroBoard.CreatedAt,
            null,
            retroBoard.VotingStrategyType.ToString());
    }
}
