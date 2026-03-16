using Api0a.WebApi.Data;
using Api0a.WebApi.DTOs;
using Api0a.WebApi.Entities;
using Api0a.WebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api0a.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for column operations within a retro board.
/// </summary>
/// <remarks>
/// DESIGN: Each handler is a Transaction Script — the entire operation
/// (validate → query → mutate → save) happens in a single method with
/// direct DbContext access. Compare with API 1 where the same operation
/// traverses: Controller → IColumnService → ColumnService →
/// IColumnRepository → ColumnRepository → IUnitOfWork → UnitOfWork.
///
/// The check-then-act race condition on column name uniqueness is the SAME
/// as API 1's ColumnService.CreateAsync. Both are vulnerable to concurrent
/// duplicate requests. Api0b fixes this at the database level.
/// </remarks>
public static class ColumnEndpoints
{
    /// <summary>Maps column-related endpoints to the application.</summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapColumnEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/retros/{retroId:guid}/columns")
            .WithTags("Columns");

        group.MapPost("/", CreateColumn);
        group.MapPut("/{columnId:guid}", UpdateColumn);
        group.MapDelete("/{columnId:guid}", DeleteColumn);
    }

    /// <summary>Creates a new column in a retro board.</summary>
    private static async Task<IResult> CreateColumn(
        Guid retroId,
        CreateColumnRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        // 1. Verify retro board exists
        _ = await db.RetroBoards.FindAsync([retroId], ct)
            ?? throw new NotFoundException("RetroBoard", retroId);

        // 2. INVARIANT: column name must be unique within retro
        //    DESIGN: This check is NOT atomic — a race condition can create
        //    duplicates. Api0b fixes this by catching the DB unique constraint
        //    violation in middleware.
        bool nameExists = await db.Columns
            .AnyAsync(c => c.RetroBoardId == retroId && c.Name == request.Name, ct);
        if (nameExists)
            throw new DuplicateException("Column", "Name", request.Name);

        // 3. Create & persist
        var column = new Column
        {
            RetroBoardId = retroId,
            Name = request.Name
        };

        db.Columns.Add(column);
        await db.SaveChangesAsync(ct);

        ColumnResponse response = new(column.Id, column.Name, null);
        return Results.Created($"/api/retros/{retroId}/columns/{column.Id}", response);
    }

    /// <summary>Updates an existing column's name.</summary>
    private static async Task<IResult> UpdateColumn(
        Guid retroId,
        Guid columnId,
        UpdateColumnRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        Column column = await db.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId, ct)
            ?? throw new NotFoundException("Column", columnId);

        // INVARIANT: new name must be unique within the retro board
        bool nameExists = await db.Columns
            .AnyAsync(c => c.RetroBoardId == retroId && c.Name == request.Name, ct);
        if (nameExists)
            throw new DuplicateException("Column", "Name", request.Name);

        column.Name = request.Name;
        await db.SaveChangesAsync(ct);

        ColumnResponse response = new(column.Id, column.Name, null);
        return Results.Ok(response);
    }

    /// <summary>Soft-deletes a column.</summary>
    private static async Task<IResult> DeleteColumn(
        Guid retroId,
        Guid columnId,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        Column column = await db.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId, ct)
            ?? throw new NotFoundException("Column", columnId);

        db.Columns.Remove(column);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
