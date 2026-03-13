using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;
using Api1.Application.Exceptions;
using Api1.Application.Interfaces;
using Api1.Domain.Entities;

namespace Api1.Application.Services;

/// <summary>
/// Service responsible for note-related business logic.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 note text uniqueness within a column is enforced here
/// using a check-then-act query — same non-atomic pattern as ColumnService.
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
        // 1. Verify column exists
        _ = await _columnRepository.GetByIdAsync(columnId, cancellationToken)
            ?? throw new NotFoundException("Column", columnId);

        // 2. INVARIANT: note text must be unique within the column
        if (await _noteRepository.ExistsByTextInColumnAsync(columnId, request.Text, cancellationToken))
            throw new DuplicateException("Note", "Text", request.Text);

        // 3. Map & persist
        var note = new Note
        {
            ColumnId = columnId,
            Text = request.Text
        };

        await _noteRepository.AddAsync(note, cancellationToken);
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

        // INVARIANT: new text must be unique within the column
        if (await _noteRepository.ExistsByTextInColumnAsync(columnId, request.Text, cancellationToken))
            throw new DuplicateException("Note", "Text", request.Text);

        note.Text = request.Text;
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
