using Api0b.WebApi.Data;
using Api0b.WebApi.DTOs;
using Api0b.WebApi.Entities;
using Api0b.WebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api0b.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for project and project membership operations.
/// </summary>
/// <remarks>
/// DESIGN: Identical to Api0a's <see cref="ProjectEndpoints"/>. No endpoint
/// handler code changed for Api0b's concurrency safety.
/// </remarks>
public static class ProjectEndpoints
{
    /// <summary>Maps project-related endpoints to the application.</summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/projects")
            .WithTags("Projects");

        group.MapPost("/", CreateProject);
        group.MapGet("/{id:guid}", GetProjectById);
        group.MapPost("/{id:guid}/members", AddMember);
        group.MapDelete("/{id:guid}/members/{userId:guid}", RemoveMember);
    }

    /// <summary>Creates a new project.</summary>
    private static async Task<IResult> CreateProject(
        CreateProjectRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        var project = new Project
        {
            Name = request.Name
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        ProjectResponse response = new(project.Id, project.Name, project.CreatedAt);
        return Results.Created($"/api/projects/{project.Id}", response);
    }

    /// <summary>Retrieves a project by its ID.</summary>
    private static async Task<IResult> GetProjectById(
        Guid id,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        Project project = await db.Projects.FindAsync([id], ct)
            ?? throw new NotFoundException("Project", id);

        ProjectResponse response = new(project.Id, project.Name, project.CreatedAt);
        return Results.Ok(response);
    }

    /// <summary>Adds a user as a member to a project.</summary>
    private static async Task<IResult> AddMember(
        Guid id,
        AddMemberRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        // Verify the project exists
        _ = await db.Projects.FindAsync([id], ct)
            ?? throw new NotFoundException("Project", id);

        // Verify the user exists
        _ = await db.Users.FindAsync([request.UserId], ct)
            ?? throw new NotFoundException("User", request.UserId);

        // Check for duplicate membership
        bool alreadyMember = await db.ProjectMembers
            .AnyAsync(pm => pm.ProjectId == id && pm.UserId == request.UserId, ct);
        if (alreadyMember)
            throw new DuplicateException("ProjectMember", "UserId", request.UserId.ToString());

        var member = new ProjectMember
        {
            ProjectId = id,
            UserId = request.UserId
        };

        db.ProjectMembers.Add(member);
        await db.SaveChangesAsync(ct);

        ProjectMemberResponse response = new(member.Id, member.ProjectId, member.UserId);
        return Results.Created($"/api/projects/{id}/members/{member.UserId}", response);
    }

    /// <summary>Removes a user from a project.</summary>
    private static async Task<IResult> RemoveMember(
        Guid id,
        Guid userId,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        ProjectMember member = await db.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == id && pm.UserId == userId, ct)
            ?? throw new NotFoundException("ProjectMember", userId);

        db.ProjectMembers.Remove(member);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
