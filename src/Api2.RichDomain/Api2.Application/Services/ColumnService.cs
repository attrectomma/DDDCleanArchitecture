using Api2.Application.DTOs.Requests;
using Api2.Application.DTOs.Responses;
using Api2.Application.Exceptions;
using Api2.Application.Interfaces;
using Api2.Domain.Entities;

namespace Api2.Application.Services;

/// <summary>
/// Orchestrates column operations by loading entities, invoking domain
/// behaviour, and persisting changes.
/// </summary>
/// <remarks>
/// DESIGN: Compare with API 1's ColumnService — the Column entity now has
/// a factory constructor and a <see cref="Column.Rename"/> method. However,
/// the uniqueness check for column names across a retro board still lives
/// here because a Column doesn't know about its siblings.
///
/// API 3 solves this by making RetroBoard the aggregate root that owns all
/// columns, so the column name uniqueness check moves into the aggregate.
///
/// DESIGN (CQRS foreshadowing): Both the create and get operations go through
/// the same service and repository. API 5 separates these into distinct
/// command/query handlers, allowing reads to bypass the domain model entirely.
/// </remarks>
public class ColumnService : IColumnService
{
    private readonly IColumnRepository _columnRepository;
    private readonly IRetroBoardRepository _retroBoardRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ColumnService"/>.
    /// </summary>
    /// <param name="columnRepository">The column repository.</param>
    /// <param name="retroBoardRepository">The retro board repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public ColumnService(
        IColumnRepository columnRepository,
        IRetroBoardRepository retroBoardRepository,
        IUnitOfWork unitOfWork)
    {
        _columnRepository = columnRepository;
        _retroBoardRepository = retroBoardRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ColumnResponse> CreateAsync(
        Guid retroBoardId,
        CreateColumnRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify retro board exists
        _ = await _retroBoardRepository.GetByIdAsync(retroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", retroBoardId);

        // 2. INVARIANT: column name must be unique within retro
        //    DESIGN: Cross-entity invariant — can't be inside Column itself.
        //    Still a check-then-act race condition (same as API 1).
        //    API 3 solves this with aggregate locking.
        if (await _columnRepository.ExistsByNameInRetroAsync(retroBoardId, request.Name, cancellationToken))
            throw new DuplicateException("Column", "Name", request.Name);

        // 3. Factory constructor validates the name
        var column = new Column(retroBoardId, request.Name);

        await _columnRepository.AddAsync(column, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ColumnResponse(column.Id, column.Name, null);
    }

    /// <inheritdoc />
    public async Task<ColumnResponse> UpdateAsync(
        Guid retroBoardId,
        Guid columnId,
        UpdateColumnRequest request,
        CancellationToken cancellationToken = default)
    {
        Column column = await _columnRepository.GetByIdAsync(columnId, cancellationToken)
            ?? throw new NotFoundException("Column", columnId);

        // INVARIANT: new name must be unique within the retro board
        if (await _columnRepository.ExistsByNameInRetroAsync(retroBoardId, request.Name, cancellationToken))
            throw new DuplicateException("Column", "Name", request.Name);

        // DESIGN: Domain method validates the name is not null/whitespace.
        column.Rename(request.Name);
        _columnRepository.Update(column);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ColumnResponse(column.Id, column.Name, null);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        Guid retroBoardId,
        Guid columnId,
        CancellationToken cancellationToken = default)
    {
        Column column = await _columnRepository.GetByIdAsync(columnId, cancellationToken)
            ?? throw new NotFoundException("Column", columnId);

        _columnRepository.Delete(column);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
