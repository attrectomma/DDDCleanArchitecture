// ── DTOs ─────────────────────────────────────────────────────────
// DESIGN: Identical to Api0a's DTOs. The concurrency safety changes in
// Api0b do not affect the REST contract — same endpoints, same request
// and response shapes. This is the whole point: the fix is invisible
// to clients.

namespace Api0b.WebApi.DTOs;

// ── Request DTOs ─────────────────────────────────────────────────

/// <summary>Request DTO for creating a new user.</summary>
/// <param name="Name">The display name of the user.</param>
/// <param name="Email">The email address of the user.</param>
public record CreateUserRequest(string Name, string Email);

/// <summary>Request DTO for creating a new project.</summary>
/// <param name="Name">The name of the project.</param>
public record CreateProjectRequest(string Name);

/// <summary>Request DTO for adding a user as a project member.</summary>
/// <param name="UserId">The ID of the user to add.</param>
public record AddMemberRequest(Guid UserId);

/// <summary>Request DTO for creating a new retro board.</summary>
/// <param name="Name">The name of the retro board.</param>
public record CreateRetroBoardRequest(string Name);

/// <summary>Request DTO for creating a new column.</summary>
/// <param name="Name">The name of the column.</param>
public record CreateColumnRequest(string Name);

/// <summary>Request DTO for updating a column's name.</summary>
/// <param name="Name">The new name for the column.</param>
public record UpdateColumnRequest(string Name);

/// <summary>Request DTO for creating a new note.</summary>
/// <param name="Text">The text content of the note.</param>
public record CreateNoteRequest(string Text);

/// <summary>Request DTO for updating a note's text.</summary>
/// <param name="Text">The new text for the note.</param>
public record UpdateNoteRequest(string Text);

/// <summary>Request DTO for casting a vote on a note.</summary>
/// <param name="UserId">The ID of the user casting the vote.</param>
public record CastVoteRequest(Guid UserId);

// ── Response DTOs ────────────────────────────────────────────────

/// <summary>Response DTO representing a user.</summary>
/// <param name="Id">The unique identifier of the user.</param>
/// <param name="Name">The display name of the user.</param>
/// <param name="Email">The email address of the user.</param>
/// <param name="CreatedAt">The UTC timestamp when the user was created.</param>
public record UserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);

/// <summary>Response DTO representing a project.</summary>
/// <param name="Id">The unique identifier of the project.</param>
/// <param name="Name">The name of the project.</param>
/// <param name="CreatedAt">The UTC timestamp when the project was created.</param>
public record ProjectResponse(Guid Id, string Name, DateTime CreatedAt);

/// <summary>Response DTO representing a project member assignment.</summary>
/// <param name="Id">The unique identifier of the membership.</param>
/// <param name="ProjectId">The ID of the project.</param>
/// <param name="UserId">The ID of the user.</param>
public record ProjectMemberResponse(Guid Id, Guid ProjectId, Guid UserId);

/// <summary>Response DTO representing a retro board with its columns.</summary>
/// <param name="Id">The unique identifier of the retro board.</param>
/// <param name="Name">The name of the retro board.</param>
/// <param name="ProjectId">The ID of the project this retro board belongs to.</param>
/// <param name="CreatedAt">The UTC timestamp when the retro board was created.</param>
/// <param name="Columns">The columns in this retro board, or <c>null</c> if not loaded.</param>
public record RetroBoardResponse(Guid Id, string Name, Guid ProjectId, DateTime CreatedAt, List<ColumnResponse>? Columns);

/// <summary>Response DTO representing a column with its notes.</summary>
/// <param name="Id">The unique identifier of the column.</param>
/// <param name="Name">The name of the column.</param>
/// <param name="Notes">The notes in this column, or <c>null</c> if not loaded.</param>
public record ColumnResponse(Guid Id, string Name, List<NoteResponse>? Notes);

/// <summary>Response DTO representing a note with its vote count.</summary>
/// <param name="Id">The unique identifier of the note.</param>
/// <param name="Text">The text content of the note.</param>
/// <param name="VoteCount">The number of votes on this note, or <c>null</c> if not loaded.</param>
public record NoteResponse(Guid Id, string Text, int? VoteCount);

/// <summary>Response DTO representing a vote.</summary>
/// <param name="Id">The unique identifier of the vote.</param>
/// <param name="NoteId">The ID of the note this vote is for.</param>
/// <param name="UserId">The ID of the user who cast this vote.</param>
public record VoteResponse(Guid Id, Guid NoteId, Guid UserId);
