using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.RetroAggregate;
using MediatR;

namespace Api5.Application.Retros.Commands.ChangeVotingStrategy;

/// <summary>
/// Handles the <see cref="ChangeVotingStrategyCommand"/> by loading the
/// RetroBoard aggregate, changing its voting strategy, and persisting it.
/// </summary>
/// <remarks>
/// DESIGN: Changing the voting strategy is a configuration change on the
/// aggregate. Existing votes are NOT re-validated because:
///   1. Votes belong to a separate aggregate (cross-aggregate invariant
///      enforcement would require loading all votes).
///   2. Votes that were valid under the previous strategy should not
///      retroactively become invalid.
///   3. In a real application, a "strategy migration" could be handled
///      by an asynchronous process if retroactive validation is needed.
///
/// This is a conscious design trade-off that's worth discussing in
/// educational contexts.
/// </remarks>
public class ChangeVotingStrategyCommandHandler
    : IRequestHandler<ChangeVotingStrategyCommand, RetroBoardResponse>
{
    private readonly IRetroBoardRepository _retroBoardRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ChangeVotingStrategyCommandHandler"/>.
    /// </summary>
    /// <param name="retroBoardRepository">The retro board repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public ChangeVotingStrategyCommandHandler(
        IRetroBoardRepository retroBoardRepository,
        IUnitOfWork unitOfWork)
    {
        _retroBoardRepository = retroBoardRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Changes the voting strategy of a retro board.
    /// </summary>
    /// <param name="request">The change voting strategy command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated retro board response.</returns>
    /// <exception cref="NotFoundException">Thrown when the retro board is not found.</exception>
    public async Task<RetroBoardResponse> Handle(
        ChangeVotingStrategyCommand request,
        CancellationToken cancellationToken)
    {
        RetroBoard retro = await _retroBoardRepository.GetByIdAsync(request.RetroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", request.RetroBoardId);

        retro.SetVotingStrategy(request.VotingStrategyType);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RetroBoardResponse(
            retro.Id,
            retro.Name,
            retro.ProjectId,
            retro.CreatedAt,
            null,
            retro.VotingStrategyType.ToString());
    }
}
