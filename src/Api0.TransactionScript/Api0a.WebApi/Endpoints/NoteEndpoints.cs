using Api0a.WebApi.Data;
using Api0a.WebApi.DTOs;
using Api0a.WebApi.Entities;
using Api0a.WebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api0a.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for note operations within a column.
/// </summary>
/// <remarks>
/// DESIGN: Same Transaction Script approach as <see cref="ColumnEndpoints"/>.
/// Note text uniqueness within a column uses the same non-atomic
/// check-then-act pattern.
/// </remarks>
public static class NoteEndpoints
{
    /// <summary>Maps note-related endpoints to the application.</summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/columns/{columnId:guid}/notes")
            .WithTags("Notes");

        group.MapPost("/", CreateNote);
        group.MapPut("/{noteId:guid}", UpdateNote);
        group.MapDelete("/{noteId:guid}", DeleteNote);
    }

    /// <summary>Creates a new note in a column.</summary>
    private static async Task<IResult> CreateNote(
        Guid columnId,
        CreateNoteRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        // 1. Verify column exists
        _ = await db.Columns.FindAsync([columnId], ct)
            ?? throw new NotFoundException("Column", columnId);

        // 2. INVARIANT: note text must be unique within the column
        bool textExists = await db.Notes
            .AnyAsync(n => n.ColumnId == columnId && n.Text == request.Text, ct);
        if (textExists)
            throw new DuplicateException("Note", "Text", request.Text);

        // 3. Create & persist
        var note = new Note
        {
            ColumnId = columnId,
            Text = request.Text
        };

        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);

        NoteResponse response = new(note.Id, note.Text, 0);
        return Results.Created($"/api/columns/{columnId}/notes/{note.Id}", response);
    }

    /// <summary>Updates an existing note's text.</summary>
    private static async Task<IResult> UpdateNote(
        Guid columnId,
        Guid noteId,
        UpdateNoteRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        Note note = await db.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId, ct)
            ?? throw new NotFoundException("Note", noteId);

        // INVARIANT: new text must be unique within the column
        bool textExists = await db.Notes
            .AnyAsync(n => n.ColumnId == columnId && n.Text == request.Text, ct);
        if (textExists)
            throw new DuplicateException("Note", "Text", request.Text);

        note.Text = request.Text;
        await db.SaveChangesAsync(ct);

        NoteResponse response = new(note.Id, note.Text, null);
        return Results.Ok(response);
    }

    /// <summary>Soft-deletes a note.</summary>
    private static async Task<IResult> DeleteNote(
        Guid columnId,
        Guid noteId,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        Note note = await db.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId, ct)
            ?? throw new NotFoundException("Note", noteId);

        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
