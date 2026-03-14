using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;
using Api2.Application.Exceptions;
using Api2.Application.Interfaces;
using Api2.Domain.Entities;
using Api2.Domain.Exceptions;

namespace Api2.Application.Services;

/// <summary>
/// Orchestrates vote operations by loading the Note entity with its votes,
/// invoking domain behaviour, and persisting changes.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 1's VoteService — the one-vote-per-user-per-note
/// invariant has moved into <see cref="Note.CastVote"/>. The service no longer
/// needs <c>IVoteRepository.ExistsAsync</c> for the duplicate check. Instead,
/// it loads the Note with its Votes collection so the domain method can perform
/// the in-memory check.
///
/// For deletion, the service loads the note with votes and calls
/// <see cref="Note.RemoveVote"/>. If the vote is not found in the collection,
/// the domain method throws <see cref="DomainException"/>, which the service
/// translates to <see cref="NotFoundException"/> for the HTTP 404 contract.
///
/// This demonstrates a common pattern in rich-domain architectures: the service
/// acts as a translator between domain exceptions and application-level exceptions.
/// </remarks>
public class VoteService : IVoteService
{
    private readonly INoteRepository _noteRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="VoteService"/>.
    /// </summary>
    /// <param name="noteRepository">The note repository (with eager-loading for votes).</param>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public VoteService(
        INoteRepository noteRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
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
        // 1. Verify user exists (application-level concern)
        _ = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        // 2. DESIGN: Must load note WITH votes for the domain check to work.
        //    This is a hidden coupling between loading strategy and domain logic.
        Note note = await _noteRepository.GetByIdWithVotesAsync(noteId, cancellationToken)
            ?? throw new NotFoundException("Note", noteId);

        // 3. Invariant enforced inside the entity — throws InvariantViolationException
        Vote vote = note.CastVote(request.UserId);

        // EF Core detects the new Vote in the Note._votes collection and adds it.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new VoteResponse(vote.Id, vote.NoteId, vote.UserId);
    }

    /// <inheritdoc />
    public async Task DeleteVoteAsync(
        Guid noteId,
        Guid voteId,
        CancellationToken cancellationToken = default)
    {
        // Load note with votes so we can use the domain method
        Note note = await _noteRepository.GetByIdWithVotesAsync(noteId, cancellationToken)
            ?? throw new NotFoundException("Note", noteId);

        // DESIGN: The domain method throws DomainException if the vote is not found
        // in the collection. We translate this to NotFoundException for the HTTP 404 contract.
        try
        {
            note.RemoveVote(voteId);
        }
        catch (DomainException)
        {
            throw new NotFoundException("Vote", voteId);
        }

        // EF Core detects the removed Vote from the Note._votes collection
        // and marks it as Deleted (required FK → orphan deletion).
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
