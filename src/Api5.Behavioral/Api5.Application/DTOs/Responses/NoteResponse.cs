namespace Api5.Application.DTOs.Responses;

/// <summary>Response DTO representing a note with its vote count.</summary>
/// <param name="Id">The unique identifier of the note.</param>
/// <param name="Text">The text content of the note.</param>
/// <param name="VoteCount">The number of votes on this note, or <c>null</c> if not computed.</param>
/// <remarks>
/// DESIGN: In API 4, VoteCount was computed by loading aggregates and then
/// querying the VoteRepository for counts — a cross-aggregate read.
/// In API 5, the CQRS query handler computes VoteCount directly in the
/// database projection query using a subquery, avoiding the need for
/// separate aggregate loading entirely.
/// </remarks>
public record NoteResponse(Guid Id, string Text, int? VoteCount);
