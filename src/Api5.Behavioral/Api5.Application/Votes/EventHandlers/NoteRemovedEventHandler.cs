using Api5.Application.Common.Interfaces;
using Api5.Domain.RetroAggregate.Events;
using Api5.Domain.VoteAggregate;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Api5.Application.Votes.EventHandlers;

/// <summary>
/// When a note is removed, clean up all associated votes.
/// </summary>
/// <remarks>
/// DESIGN: In API 4, vote cleanup when a note is deleted was handled by
/// either:
///   - DB cascade delete (FK from Vote to Note with ON DELETE CASCADE), or
///   - Explicit code in the service layer.
///
/// With domain events, the cleanup is decoupled. The RetroBoard aggregate
/// raises <see cref="NoteRemovedEvent"/>, and this handler reacts without
/// the retro aggregate knowing anything about votes. This is the
/// Open/Closed Principle: new reactions to note removal can be added
/// as additional <c>INotificationHandler&lt;NoteRemovedEvent&gt;</c>
/// implementations without modifying the RetroBoard aggregate or the
/// RemoveNote command handler.
///
/// TRANSACTIONAL SAFETY: This handler runs within the same database
/// transaction as the command that triggered the event. The
/// <c>TransactionBehavior</c> pipeline behavior opens an explicit
/// transaction before the command handler runs. The
/// <c>DomainEventInterceptor</c> dispatches events after
/// <c>SaveChangesAsync</c>, but the explicit transaction is still open.
/// When this handler calls <c>SaveChangesAsync</c> to delete votes, those
/// changes participate in the same transaction. If this handler fails,
/// the entire transaction rolls back — including the original note deletion.
/// </remarks>
public class NoteRemovedEventHandler : INotificationHandler<NoteRemovedEvent>
{
    private readonly IVoteRepository _voteRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<NoteRemovedEventHandler> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="NoteRemovedEventHandler"/>.
    /// </summary>
    /// <param name="voteRepository">The vote repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    /// <param name="logger">The logger instance.</param>
    public NoteRemovedEventHandler(
        IVoteRepository voteRepository,
        IUnitOfWork unitOfWork,
        ILogger<NoteRemovedEventHandler> logger)
    {
        _voteRepository = voteRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Cleans up all votes associated with the removed note.
    /// </summary>
    /// <param name="notification">The note removed event.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public async Task Handle(NoteRemovedEvent notification, CancellationToken cancellationToken)
    {
        List<Vote> votes = await _voteRepository.GetByNoteIdAsync(notification.NoteId, cancellationToken);

        if (votes.Count == 0)
            return;

        _logger.LogInformation(
            "Cleaning up {VoteCount} vote(s) for removed note {NoteId}",
            votes.Count,
            notification.NoteId);

        foreach (Vote vote in votes)
        {
            _voteRepository.Delete(vote);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
