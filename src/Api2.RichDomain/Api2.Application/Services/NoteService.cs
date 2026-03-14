using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;
using Api2.Application.Exceptions;
using Api2.Application.Interfaces;
using Api2.Domain.Entities;

namespace Api2.Application.Services;

/// <summary>
/// Orchestrates note operations by loading entities, invoking domain
/// behaviour, and persisting changes.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 1's NoteService — note creation now delegates
/// the unique-text invariant to <see cref="Column.AddNote"/>. The service
/// loads the Column with its Notes collection so the domain method can
/// perform the in-memory check. This is a hidden coupling between loading
/// strategy and domain logic.
///
/// For updates, the service still checks text uniqueness via a repository
/// query because <see cref="Note.UpdateText"/> has no access to sibling notes.
/// API 3 resolves all of these by loading the entire RetroBoard aggregate.
/// </remarks>
public class NoteService : INoteService
{
    private readonly INoteRepository _noteRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="NoteService"/>.
    /// </summary>
    /// <param name="noteRepository">The note repository.</param>
    /// <param name="columnRepository">The column repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public NoteService(
        INoteRepository noteRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork)
    {
        _noteRepository = noteRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<NoteResponse> CreateAsync(
        Guid columnId,
        CreateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        // DESIGN: Must load column WITH notes for the domain check to work.
        // This is a hidden coupling between loading strategy and domain logic.
        Column column = await _columnRepository.GetByIdWithNotesAsync(columnId, cancellationToken)
            ?? throw new NotFoundException("Column", columnId);

        // Invariant enforced inside the entity — throws InvariantViolationException
        // if a note with the same text already exists in this column.
        Note note = column.AddNote(request.Text);

        // EF Core detects the new Note in the Column._notes collection and adds it.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new NoteResponse(note.Id, note.Text, 0);
    }

    /// <inheritdoc />
    public async Task<NoteResponse> UpdateAsync(
        Guid columnId,
        Guid noteId,
        UpdateNoteRequest request,
        CancellationToken cancellationToken = default)
    {
        Note note = await _noteRepository.GetByIdAsync(noteId, cancellationToken)
            ?? throw new NotFoundException("Note", noteId);

        // DESIGN: Cross-entity uniqueness check still in service because
        // Note.UpdateText has no access to sibling notes.
        if (await _noteRepository.ExistsByTextInColumnAsync(columnId, request.Text, cancellationToken))
            throw new DuplicateException("Note", "Text", request.Text);

        // Domain method validates the new text is not null/whitespace.
        note.UpdateText(request.Text);
        _noteRepository.Update(note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new NoteResponse(note.Id, note.Text, null);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        Guid columnId,
        Guid noteId,
        CancellationToken cancellationToken = default)
    {
        Note note = await _noteRepository.GetByIdAsync(noteId, cancellationToken)
            ?? throw new NotFoundException("Note", noteId);

        _noteRepository.Delete(note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
