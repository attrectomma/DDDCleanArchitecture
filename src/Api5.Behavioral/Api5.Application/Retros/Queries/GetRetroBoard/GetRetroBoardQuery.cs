using Api5.Application.DTOs.Responses;
using MediatR;

namespace Api5.Application.Retros.Queries.GetRetroBoard;

/// <summary>
/// Query to retrieve a retro board with all its columns, notes, and vote counts.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a READ operation. It does NOT load the
/// RetroBoard aggregate — instead the handler projects directly from the
/// database using a no-tracking query. This is dramatically cheaper
/// than the API 3/4 approach of hydrating the full aggregate graph.
/// </remarks>
/// <param name="RetroBoardId">The unique identifier of the retro board.</param>
public record GetRetroBoardQuery(Guid RetroBoardId) : IRequest<RetroBoardResponse>;
