using Api0b.WebApi.Data;
using Api0b.WebApi.DTOs;
using Api0b.WebApi.Entities;
using Api0b.WebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api0b.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for user operations.
/// </summary>
/// <remarks>
/// DESIGN: Identical to Api0a's <see cref="UserEndpoints"/>. The concurrency
/// safety changes in Api0b are purely in entity configuration (xmin tokens)
/// and middleware (catch blocks). No endpoint handler code changed.
/// </remarks>
public static class UserEndpoints
{
    /// <summary>Maps user-related endpoints to the application.</summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/users")
            .WithTags("Users");

        group.MapPost("/", CreateUser);
        group.MapGet("/{id:guid}", GetUserById);
    }

    /// <summary>Creates a new user.</summary>
    private static async Task<IResult> CreateUser(
        CreateUserRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        var user = new User
        {
            Name = request.Name,
            Email = request.Email
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        UserResponse response = new(user.Id, user.Name, user.Email, user.CreatedAt);
        return Results.Created($"/api/users/{user.Id}", response);
    }

    /// <summary>Retrieves a user by their ID.</summary>
    private static async Task<IResult> GetUserById(
        Guid id,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        User user = await db.Users.FindAsync([id], ct)
            ?? throw new NotFoundException("User", id);

        UserResponse response = new(user.Id, user.Name, user.Email, user.CreatedAt);
        return Results.Ok(response);
    }
}
