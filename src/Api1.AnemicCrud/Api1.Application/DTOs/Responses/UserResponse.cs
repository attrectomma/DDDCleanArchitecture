namespace Api1.Application.DTOs.Responses;

/// <summary>Response DTO representing a user.</summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Name">The display name of the user.</param>
/// <param name="Email">The email address of the user.</param>
/// <param name="CreatedAt">The UTC timestamp when the user was created.</param>
public record UserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
