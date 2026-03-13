using Api1.Application.DTOs.Requests;
using Api1.Application.DTOs.Responses;
using Api1.Application.Exceptions;
using Api1.Application.Interfaces;
using Api1.Domain.Entities;

namespace Api1.Application.Services;

/// <summary>
/// Service responsible for all Column-related business logic.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 all invariant checks live here in the service layer.
/// The domain entity <see cref="Column"/> is a plain DTO with no behaviour.
/// This is the "anemic domain model" anti-pattern — common in junior codebases
/// but problematic because business rules are scattered across services.
/// See API 2 where these checks move into the entity itself.
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
        //    DESIGN: This check is NOT atomic — a race condition can
        //    create duplicates. API 3 solves this with aggregate locking.
        if (await _columnRepository.ExistsByNameInRetroAsync(retroBoardId, request.Name, cancellationToken))
            throw new DuplicateException("Column", "Name", request.Name);

        // 3. Map & persist
        var column = new Column
        {
            RetroBoardId = retroBoardId,
            Name = request.Name
        };

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

        column.Name = request.Name;
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
