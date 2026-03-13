using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;
using Api1.Application.Exceptions;
using Api1.Application.Interfaces;
using Api1.Domain.Entities;

namespace Api1.Application.Services;

/// <summary>
/// Service responsible for vote-related business logic.
/// </summary>
/// <remarks>
/// DESIGN: The one-vote-per-user-per-note invariant is checked via a query
/// before insert — NOT safe under concurrency. Two simultaneous requests
/// can both pass the check and create duplicate votes.
/// API 3+ enforce this inside the aggregate boundary with a DB unique constraint
/// as the ultimate safety net and aggregate-level concurrency tokens for proper control.
/// </remarks>
public class VoteService : IVoteService
{
    private readonly IVoteRepository _voteRepository;
    private readonly INoteRepository _noteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="VoteService"/>.
    /// </summary>
    /// <param name="voteRepository">The vote repository.</param>
    /// <param name="noteRepository">The note repository.</param>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public VoteService(
        IVoteRepository voteRepository,
        INoteRepository noteRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _voteRepository = voteRepository;
        _noteRepository = noteRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<VoteResponse> CastVoteAsync(
        Guid noteId,
        CastVoteRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify note exists
        _ = await _noteRepository.GetByIdAsync(noteId, cancellationToken)
            ?? throw new NotFoundException("Note", noteId);

        // 2. Verify user exists
        _ = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // 3. INVARIANT: one vote per user per note
        //    DESIGN: Checked via a query before insert — NOT safe under concurrency.
        if (await _voteRepository.ExistsAsync(noteId, request.UserId, cancellationToken))
            throw new BusinessRuleException("User has already voted on this note.");

        // 4. Create the vote
        var vote = new Vote
        {
            NoteId = noteId,
            UserId = request.UserId
        };

        await _voteRepository.AddAsync(vote, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }

    /// <inheritdoc />
    public async Task DeleteVoteAsync(
        Guid noteId,
        Guid voteId,
        CancellationToken cancellationToken = default)
    {
        Vote vote = await _voteRepository.GetByIdAsync(voteId, cancellationToken)
            ?? throw new NotFoundException("Vote", voteId);

        _voteRepository.Delete(vote);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
