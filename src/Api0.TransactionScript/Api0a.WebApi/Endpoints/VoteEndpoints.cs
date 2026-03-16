using Api0a.WebApi.Data;
using Api0a.WebApi.DTOs;
using Api0a.WebApi.Entities;
using Api0a.WebApi.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Api0a.WebApi.Endpoints;

/// <summary>
/// Minimal API endpoints for vote operations on notes.
/// </summary>
/// <remarks>
/// DESIGN: The one-vote-per-user-per-note invariant is checked via a query
/// before insert — NOT safe under concurrency. Two simultaneous requests
/// can both pass the check and create duplicate votes. Api0b fixes this
/// by catching the DB unique constraint violation in middleware.
/// </remarks>
public static class VoteEndpoints
{
    /// <summary>Maps vote-related endpoints to the application.</summary>
    /// <param name="app">The endpoint route builder.</param>
    public static void MapVoteEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/notes/{noteId:guid}/votes")
            .WithTags("Votes");

        group.MapPost("/", CastVote);
        group.MapDelete("/{voteId:guid}", DeleteVote);
    }

    /// <summary>Casts a vote on a note.</summary>
    private static async Task<IResult> CastVote(
        Guid noteId,
        CastVoteRequest request,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        // 1. Verify note exists
        _ = await db.Notes.FindAsync([noteId], ct)
            ?? throw new NotFoundException("Note", noteId);

        // 2. Verify user exists
        _ = await db.Users.FindAsync([request.UserId], ct)
            ?? throw new NotFoundException("User", request.UserId);

        // 3. INVARIANT: one vote per user per note
        //    DESIGN: Checked via query before insert — NOT safe under concurrency.
        bool alreadyVoted = await db.Votes
            .AnyAsync(v => v.NoteId == noteId && v.UserId == request.UserId, ct);
        if (alreadyVoted)
            throw new BusinessRuleException("User has already voted on this note.");

        // 4. Create the vote
        var vote = new Vote
        {
            NoteId = noteId,
            UserId = request.UserId
        };

        db.Votes.Add(vote);
        await db.SaveChangesAsync(ct);

        VoteResponse response = new(vote.Id, vote.NoteId, vote.UserId);
        return Results.Created($"/api/notes/{noteId}/votes/{vote.Id}", response);
    }

    /// <summary>Removes a vote.</summary>
    private static async Task<IResult> DeleteVote(
        Guid noteId,
        Guid voteId,
        RetroBoardDbContext db,
        CancellationToken ct)
    {
        Vote vote = await db.Votes
            .FirstOrDefaultAsync(v => v.Id == voteId, ct)
            ?? throw new NotFoundException("Vote", voteId);

        db.Votes.Remove(vote);
        await db.SaveChangesAsync(ct);

        return Results.NoContent();
    }
}
