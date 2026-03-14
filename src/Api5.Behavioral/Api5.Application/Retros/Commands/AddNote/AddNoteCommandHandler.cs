using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.RetroAggregate;
using MediatR;

namespace Api5.Application.Retros.Commands.AddNote;

/// <summary>
/// Handles the <see cref="AddNoteCommand"/> by loading the RetroBoard
/// aggregate that contains the column, adding a note, and persisting.
/// </summary>
/// <remarks>
/// DESIGN: The command uses <c>ColumnId</c> to locate the retro board
/// via <c>GetByColumnIdAsync</c>, then delegates to the aggregate's
/// <c>AddNote</c> method which enforces the note text uniqueness invariant.
/// </remarks>
public class AddNoteCommandHandler : IRequestHandler<AddNoteCommand, NoteResponse>
{
    private readonly IRetroBoardRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="AddNoteCommandHandler"/>.
    /// </summary>
    /// <param name="repository">The retro board repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public AddNoteCommandHandler(IRetroBoardRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Adds a note to a column within the retro board aggregate.
    /// </summary>
    /// <param name="request">The add note command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created note response.</returns>
    /// <exception cref="NotFoundException">Thrown when the column is not found.</exception>
    public async Task<NoteResponse> Handle(AddNoteCommand request, CancellationToken cancellationToken)
    {
        RetroBoard retro = await _repository.GetByColumnIdAsync(request.ColumnId, cancellationToken)
            ?? throw new NotFoundException("Column", request.ColumnId);

        Note note = retro.AddNote(request.ColumnId, request.Text);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new NoteResponse(note.Id, note.Text, 0);
    }
}
