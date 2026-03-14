using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api5.Application.Retros.Queries.GetRetroBoard;

/// <summary>
/// Handles the <see cref="GetRetroBoardQuery"/> by projecting directly from
/// the database — no aggregate hydration, no change tracking.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a READ operation. It does NOT load the
/// RetroBoard aggregate — instead it projects directly from the
/// DbContext using a no-tracking query. This is dramatically cheaper
/// than the API 3/4 approach of hydrating the full aggregate graph.
///
/// Compare with API 3's RetroBoardService.GetByIdAsync which loaded:
///   <c>.Include(r => r.Columns).ThenInclude(c => c.Notes).ThenInclude(n => n.Votes)</c>
/// That query had change-tracking overhead and built a full entity graph
/// only to immediately map it to a DTO. Here we skip the middleman.
///
/// Vote counts are computed via a subquery in the projection rather than
/// through a separate repository call (API 4's batch vote count approach).
/// This means a single efficient SQL query replaces what was previously
/// 2+ queries in API 4.
/// </remarks>
public class GetRetroBoardQueryHandler : IRequestHandler<GetRetroBoardQuery, RetroBoardResponse>
{
    private readonly IReadOnlyDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="GetRetroBoardQueryHandler"/>.
    /// </summary>
    /// <param name="context">The read-only database context.</param>
    public GetRetroBoardQueryHandler(IReadOnlyDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Projects a retro board with columns, notes, and vote counts
    /// directly from the database.
    /// </summary>
    /// <param name="request">The get retro board query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The retro board response with full details.</returns>
    /// <exception cref="NotFoundException">Thrown when the retro board is not found.</exception>
    public async Task<RetroBoardResponse> Handle(GetRetroBoardQuery request, CancellationToken cancellationToken)
    {
        // DESIGN (CQRS): No-tracking + Select projection.
        // EF Core generates an optimized SQL query that only retrieves
        // the columns we actually need for the response DTO.
        // Vote counts are computed via a correlated subquery — no need
        // for a separate VoteRepository.GetVoteCountsByNoteIdsAsync call
        // like in API 4.
        RetroBoardResponse? result = await _context.RetroBoards
            .AsNoTracking()
            .Where(r => r.Id == request.RetroBoardId)
            .Select(r => new RetroBoardResponse(
                r.Id,
                r.Name,
                r.ProjectId,
                r.CreatedAt,
                r.Columns.Select(c => new ColumnResponse(
                    c.Id,
                    c.Name,
                    c.Notes.Select(n => new NoteResponse(
                        n.Id,
                        n.Text,
                        _context.Votes.Count(v => v.NoteId == n.Id)
                    )).ToList()
                )).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return result ?? throw new NotFoundException("RetroBoard", request.RetroBoardId);
    }
}
