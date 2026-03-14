using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using Api5.Domain.RetroAggregate;
using MediatR;

namespace Api5.Application.Retros.Commands.RenameColumn;

/// <summary>
/// Handles the <see cref="RenameColumnCommand"/> by loading the RetroBoard
/// aggregate, renaming the column, and persisting the change.
/// </summary>
public class RenameColumnCommandHandler : IRequestHandler<RenameColumnCommand, ColumnResponse>
{
    private readonly IRetroBoardRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="RenameColumnCommandHandler"/>.
    /// </summary>
    /// <param name="repository">The retro board repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public RenameColumnCommandHandler(IRetroBoardRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Renames a column within the retro board aggregate.
    /// </summary>
    /// <param name="request">The rename column command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated column response.</returns>
    /// <exception cref="NotFoundException">Thrown when the retro board is not found.</exception>
    public async Task<ColumnResponse> Handle(RenameColumnCommand request, CancellationToken cancellationToken)
    {
        RetroBoard retro = await _repository.GetByIdAsync(request.RetroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", request.RetroBoardId);

        retro.RenameColumn(request.ColumnId, request.NewName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        Column column = retro.Columns.First(c => c.Id == request.ColumnId);
        return new ColumnResponse(column.Id, column.Name, null);
    }
}
