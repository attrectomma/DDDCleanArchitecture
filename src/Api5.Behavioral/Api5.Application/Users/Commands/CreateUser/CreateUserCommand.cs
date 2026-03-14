using Api5.Application.DTOs.Responses;
using MediatR;

namespace Api5.Application.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user.
/// </summary>
/// <remarks>
/// DESIGN: Commands are simple immutable data objects that describe the
/// user's INTENT. This is the key mindset shift from noun-centric
/// (UserService.Create) to behavior-centric (CreateUserCommand).
/// The command name reads as a sentence: "Create a user with name X and email Y."
///
/// Compare with API 4 where the controller called
/// <c>IUserService.CreateAsync(CreateUserRequest)</c>. Now the controller
/// creates this command and sends it via <c>IMediator.Send()</c>.
/// </remarks>
/// <param name="Name">The display name of the user.</param>
/// <param name="Email">The email address of the user.</param>
public record CreateUserCommand(string Name, string Email) : IRequest<UserResponse>;
