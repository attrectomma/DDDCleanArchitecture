namespace Api5.Application.DTOs.Requests;

/// <summary>Request DTO for creating a new user.</summary>
/// <param name="Name">The display name of the user.</param>
/// <param name="Email">The email address of the user.</param>
/// <remarks>
/// DESIGN: In API 5, validation moves from request DTO validators
/// to command validators. The controller maps this DTO to a
/// <c>CreateUserCommand</c>, and the <c>ValidationBehavior</c>
/// pipeline validates the command before the handler executes.
/// </remarks>
public record CreateUserRequest(string Name, string Email);
