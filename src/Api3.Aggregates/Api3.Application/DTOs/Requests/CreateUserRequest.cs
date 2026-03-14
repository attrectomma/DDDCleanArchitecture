namespace Api3.Application.DTOs.Requests;

/// <summary>Request DTO for creating a new user.</summary>
/// <param name="Name">The display name of the user.</param>
/// <param name="Email">The email address of the user.</param>
public record CreateUserRequest(string Name, string Email);
