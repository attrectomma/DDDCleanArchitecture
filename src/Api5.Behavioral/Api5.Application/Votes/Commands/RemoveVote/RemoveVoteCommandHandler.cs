using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Domain.VoteAggregate;
using MediatR;

namespace Api5.Application.Votes.Commands.RemoveVote;

/// <summary>
/// Handles the <see cref="RemoveVoteCommand"/> by loading the Vote aggregate
/// and soft-deleting it.
/// </summary>
public class RemoveVoteCommandHandler : IRequestHandler<RemoveVoteCommand, Unit>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="RemoveVoteCommandHandler"/>.
    /// </summary>
    /// <param name="voteRepository">The vote repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public RemoveVoteCommandHandler(IVoteRepository voteRepository, IUnitOfWork unitOfWork)
    {
        _voteRepository = voteRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Removes a vote aggregate.
    /// </summary>
    /// <param name="request">The remove vote command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see cref="Unit.Value"/> on success.</returns>
    /// <exception cref="NotFoundException">Thrown when the vote is not found.</exception>
    public async Task<Unit> Handle(RemoveVoteCommand request, CancellationToken cancellationToken)
    {
        Vote vote = await _voteRepository.GetByIdAsync(request.VoteId, cancellationToken)
            ?? throw new NotFoundException("Vote", request.VoteId);

        _voteRepository.Delete(vote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
