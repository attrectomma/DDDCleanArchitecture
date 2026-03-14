using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.Common.Options;
using Api5.Application.DTOs.Responses;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.RetroAggregate;
using MediatR;
using Microsoft.Extensions.Options;

namespace Api5.Application.Retros.Commands.CreateRetroBoard;

/// <summary>
/// Handles the <see cref="CreateRetroBoardCommand"/> by verifying the project
/// exists, creating a RetroBoard aggregate, and persisting it.
/// </summary>
/// <remarks>
/// DESIGN: When the command does not specify a voting strategy, the handler
/// uses the default from <see cref="VotingOptions.DefaultVotingStrategy"/>.
/// This value is configured in <c>appsettings.json</c> via the Options pattern,
/// making the default strategy externally configurable without code changes.
///
/// Compare with the controller, which also resolves the default — but the
/// handler is the authoritative source because it receives the validated
/// options instance.
/// </remarks>
public class CreateRetroBoardCommandHandler : IRequestHandler<CreateRetroBoardCommand, RetroBoardResponse>
{
    private readonly IRetroBoardRepository _retroBoardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly VotingOptions _votingOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="CreateRetroBoardCommandHandler"/>.
    /// </summary>
    /// <param name="retroBoardRepository">The retro board repository.</param>
    /// <param name="projectRepository">The project repository (for existence checks).</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    /// <param name="votingOptions">
    /// The voting configuration options, providing the default strategy.
    /// Injected via the Options pattern.
    /// </param>
    public CreateRetroBoardCommandHandler(
        IRetroBoardRepository retroBoardRepository,
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        IOptions<VotingOptions> votingOptions)
    {
        _retroBoardRepository = retroBoardRepository;
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _votingOptions = votingOptions.Value;
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

        // DESIGN: If the caller did not specify a strategy, use the configured
        // default from VotingOptions (Options pattern). This allows operators
        // to control the default behaviour via appsettings.json without
        // changing code.
        Domain.VoteAggregate.Strategies.VotingStrategyType strategyType =
            request.VotingStrategyType ?? _votingOptions.DefaultVotingStrategy;

        var retroBoard = new RetroBoard(request.ProjectId, request.Name, strategyType);

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
