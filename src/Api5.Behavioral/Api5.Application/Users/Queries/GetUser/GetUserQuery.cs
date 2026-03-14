using Api5.Application.DTOs.Responses;
using MediatR;

namespace Api5.Application.Users.Queries.GetUser;

/// <summary>
/// Query to retrieve a user by their ID.
/// </summary>
/// <remarks>
/// DESIGN (CQRS): This is a READ operation. It does NOT load the User
/// aggregate through the repository — instead, the handler projects
/// directly from the DbContext using a no-tracking query. This is the
/// core CQRS insight: reads and writes have different requirements.
///
/// Compare with API 4's <c>UserService.GetByIdAsync</c> which used the
/// repository to load the full User aggregate with change tracking,
/// only to immediately map it to a DTO and discard it.
/// </remarks>
/// <param name="UserId">The unique identifier of the user to retrieve.</param>
public record GetUserQuery(Guid UserId) : IRequest<UserResponse>;
