using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.RetroAggregate;
using MediatR;

namespace Api5.Application.Retros.Commands.AddColumn;

/// <summary>
/// Handles the <see cref="AddColumnCommand"/> by loading the RetroBoard
/// aggregate, adding a column, and persisting the change.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a WRITE operation. We MUST load the aggregate
/// through the repository so the aggregate root can enforce invariants
/// (e.g., unique column names). The aggregate is the write model.
///
/// Compare with API 4's <c>RetroBoardService.AddColumnAsync</c> — the
/// logic is identical, but here each use case has its own handler.
/// </remarks>
public class AddColumnCommandHandler : IRequestHandler<AddColumnCommand, ColumnResponse>
{
    private readonly IRetroBoardRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="AddColumnCommandHandler"/>.
    /// </summary>
    /// <param name="repository">The retro board repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public AddColumnCommandHandler(IRetroBoardRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Adds a column to the retro board aggregate.
    /// </summary>
    /// <param name="request">The add column command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created column response.</returns>
    /// <exception cref="NotFoundException">Thrown when the retro board is not found.</exception>
    public async Task<ColumnResponse> Handle(AddColumnCommand request, CancellationToken cancellationToken)
    {
        RetroBoard retro = await _repository.GetByIdAsync(request.RetroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", request.RetroBoardId);

        Column column = retro.AddColumn(request.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ColumnResponse(column.Id, column.Name, null);
    }
}
