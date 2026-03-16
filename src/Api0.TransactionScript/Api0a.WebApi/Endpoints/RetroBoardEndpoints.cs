using Api0a.WebApi.Data;
using Api0a.WebApi.DTOs;
using Api0a.WebApi.Entities;
using Api0a.WebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api0a.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for retro board operations.
/// </summary>
public static class RetroBoardEndpoints
{
    /// <summary>Maps retro board endpoints to the application.</summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapRetroBoardEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/projects/{projectId:guid}/retros")
            .WithTags("RetroBoards");

        group.MapPost("/", CreateRetroBoard);
        group.MapGet("/{retroId:guid}", GetRetroBoardById);
    }

    /// <summary>Creates a new retro board within a project.</summary>
    private static async Task<IResult> CreateRetroBoard(
        Guid projectId,
        CreateRetroBoardRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        // Verify the project exists
        _ = await db.Projects.FindAsync([projectId], ct)
            ?? throw new NotFoundException("Project", projectId);

        var retroBoard = new RetroBoard
        {
            ProjectId = projectId,
            Name = request.Name
        };

        db.RetroBoards.Add(retroBoard);
        await db.SaveChangesAsync(ct);

        RetroBoardResponse response = new(retroBoard.Id, retroBoard.Name, retroBoard.ProjectId, retroBoard.CreatedAt, null);
        return Results.Created($"/api/projects/{projectId}/retros/{retroBoard.Id}", response);
    }

    /// <summary>Retrieves a retro board by ID, including columns, notes, and vote counts.</summary>
    private static async Task<IResult> GetRetroBoardById(
        Guid projectId,
        Guid retroId,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        RetroBoard? retroBoard = await db.RetroBoards
            .Include(r => r.Columns)
                .ThenInclude(c => c.Notes)
                    .ThenInclude(n => n.Votes)
            .FirstOrDefaultAsync(r => r.Id == retroId, ct);

        if (retroBoard is null)
            throw new NotFoundException("RetroBoard", retroId);

        List<ColumnResponse> columns = retroBoard.Columns
            .Select(c => new ColumnResponse(
                c.Id,
                c.Name,
                c.Notes.Select(n => new NoteResponse(
                    n.Id,
                    n.Text,
                    n.Votes.Count
                )).ToList()
            )).ToList();

        RetroBoardResponse response = new(retroBoard.Id, retroBoard.Name, retroBoard.ProjectId, retroBoard.CreatedAt, columns);
        return Results.Ok(response);
    }
}
