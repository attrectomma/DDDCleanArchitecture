using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Domain.RetroAggregate;
using MediatR;

namespace Api5.Application.Retros.Commands.RemoveNote;

/// <summary>
/// Handles the <see cref="RemoveNoteCommand"/> by loading the RetroBoard
/// aggregate, removing the note, and persisting.
/// </summary>
/// <remarks>
/// DESIGN: The aggregate root raises a <c>NoteRemovedEvent</c> which is
/// dispatched after save by the <c>DomainEventInterceptor</c>. The
/// <c>NoteRemovedEventHandler</c> reacts by cleaning up orphaned votes.
///
/// Compare with API 4 where vote cleanup was either:
///   - Handled by DB cascade delete (FK from Vote to Note), or
///   - Explicit code in the service layer.
/// Domain events make this coupling explicit and decoupled.
/// </remarks>
public class RemoveNoteCommandHandler : IRequestHandler<RemoveNoteCommand, Unit>
{
    private readonly IRetroBoardRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="RemoveNoteCommandHandler"/>.
    /// </summary>
    /// <param name="repository">The retro board repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public RemoveNoteCommandHandler(IRetroBoardRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Removes a note from the retro board aggregate.
    /// </summary>
    /// <param name="request">The remove note command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see cref="Unit.Value"/> on success.</returns>
    /// <exception cref="NotFoundException">Thrown when the column is not found.</exception>
    public async Task<Unit> Handle(RemoveNoteCommand request, CancellationToken cancellationToken)
    {
        RetroBoard retro = await _repository.GetByColumnIdAsync(request.ColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", request.ColumnId);

        retro.RemoveNote(request.ColumnId, request.NoteId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
