using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.RetroAggregate;
using MediatR;

namespace Api5.Application.Retros.Commands.UpdateNote;

/// <summary>
/// Handles the <see cref="UpdateNoteCommand"/> by loading the RetroBoard
/// aggregate that contains the column, updating the note, and persisting.
/// </summary>
public class UpdateNoteCommandHandler : IRequestHandler<UpdateNoteCommand, NoteResponse>
{
    private readonly IRetroBoardRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdateNoteCommandHandler"/>.
    /// </summary>
    /// <param name="repository">The retro board repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public UpdateNoteCommandHandler(IRetroBoardRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Updates a note's text within the retro board aggregate.
    /// </summary>
    /// <param name="request">The update note command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated note response.</returns>
    /// <exception cref="NotFoundException">Thrown when the column is not found.</exception>
    public async Task<NoteResponse> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
    {
        RetroBoard retro = await _repository.GetByColumnIdAsync(request.ColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", request.ColumnId);

        retro.UpdateNote(request.ColumnId, request.NoteId, request.NewText);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Column column = retro.Columns.First(c => c.Id == request.ColumnId);
        Note note = column.Notes.First(n => n.Id == request.NoteId);
        return new NoteResponse(note.Id, note.Text, null);
    }
}
