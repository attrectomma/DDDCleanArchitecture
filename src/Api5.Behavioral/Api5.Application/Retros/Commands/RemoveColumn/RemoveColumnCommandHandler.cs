using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Domain.RetroAggregate;
using MediatR;

namespace Api5.Application.Retros.Commands.RemoveColumn;

/// <summary>
/// Handles the <see cref="RemoveColumnCommand"/> by loading the RetroBoard
/// aggregate, removing the column, and persisting the change.
/// </summary>
public class RemoveColumnCommandHandler : IRequestHandler<RemoveColumnCommand, Unit>
{
    private readonly IRetroBoardRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="RemoveColumnCommandHandler"/>.
    /// </summary>
    /// <param name="repository">The retro board repository.</param>
    /// <param name="unitOfWork">The unit of work for persistence.</param>
    public RemoveColumnCommandHandler(IRetroBoardRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Removes a column from the retro board aggregate.
    /// </summary>
    /// <param name="request">The remove column command.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see cref="Unit.Value"/> on success.</returns>
    /// <exception cref="NotFoundException">Thrown when the retro board is not found.</exception>
    public async Task<Unit> Handle(RemoveColumnCommand request, CancellationToken cancellationToken)
    {
        RetroBoard retro = await _repository.GetByIdAsync(request.RetroBoardId, cancellationToken)
            ?? throw new NotFoundException("RetroBoard", request.RetroBoardId);

        retro.RemoveColumn(request.ColumnId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
