using Api5.Application.Common.Exceptions;
using Api5.Application.Common.Interfaces;
using Api5.Application.DTOs.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Api5.Application.Users.Queries.GetUser;

/// <summary>
/// Handles the <see cref="GetUserQuery"/> by projecting directly from
/// the database — no aggregate loading, no change tracking.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a READ operation. The handler depends on
/// <see cref="IReadOnlyDbContext"/>, NOT on <c>IUserRepository</c>.
/// The repository's job is to load and persist AGGREGATES — it's a
/// write-side concept. Queries have different needs:
///   - No change tracking (we're not going to save anything)
///   - Custom projections (we need a different shape than the aggregate)
///   - Potentially different data sources in the future (read replicas)
///
/// Compare with API 4's <c>UserService.GetByIdAsync</c> which loaded
/// the full User aggregate through the repository with change tracking.
/// </remarks>
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserResponse>
{
    private readonly IReadOnlyDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="GetUserQueryHandler"/>.
    /// </summary>
    /// <param name="context">The read-only database context.</param>
    public GetUserQueryHandler(IReadOnlyDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Projects a user directly from the database as a response DTO.
    /// </summary>
    /// <param name="request">The get user query.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user response.</returns>
    /// <exception cref="NotFoundException">Thrown when the user is not found.</exception>
    public async Task<UserResponse> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // DESIGN (CQRS): No-tracking + Select projection.
        // EF Core generates an optimized SQL query that only retrieves
        // the columns we actually need for the response DTO.
        UserResponse? result = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserResponse(u.Id, u.Name, u.Email, u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return result ?? throw new NotFoundException("User", request.UserId);
    }
}
