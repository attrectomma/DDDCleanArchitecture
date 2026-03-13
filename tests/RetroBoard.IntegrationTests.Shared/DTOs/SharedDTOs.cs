namespace RetroBoard.IntegrationTests.Shared.DTOs;

/// <summary>
/// Shared request/response DTOs used by integration tests across all APIs.
/// These mirror the API contracts and are intentionally decoupled from the
/// API-specific DTO classes to keep tests independent of implementation.
/// </summary>
/// <remarks>
/// DESIGN: Tests use their own DTO definitions rather than referencing
/// the API project's DTOs. This ensures the tests validate the API's
/// actual JSON contract, not just that the same C# types serialize correctly.
/// If an API changes a property name, the test will catch the mismatch.
/// </remarks>

// ── Users ────────────────────────────────────────────────────────

public record CreateUserRequest(string Name, string Email);

public record UserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);

// ── Projects ─────────────────────────────────────────────────────

public record CreateProjectRequest(string Name);

public record ProjectResponse(Guid Id, string Name, DateTime CreatedAt);

public record AddMemberRequest(Guid UserId);

// ── Retro Boards ─────────────────────────────────────────────────

public record CreateRetroBoardRequest(string Name);

public record RetroBoardResponse(
    Guid Id,
    string Name,
    Guid ProjectId,
    DateTime CreatedAt,
    List<ColumnResponse>? Columns);

// ── Columns ──────────────────────────────────────────────────────

public record CreateColumnRequest(string Name);

public record UpdateColumnRequest(string Name);

public record ColumnResponse(Guid Id, string Name, List<NoteResponse>? Notes);

// ── Notes ────────────────────────────────────────────────────────

public record CreateNoteRequest(string Text);

public record UpdateNoteRequest(string Text);

public record NoteResponse(Guid Id, string Text, int? VoteCount);

// ── Votes ────────────────────────────────────────────────────────

public record CastVoteRequest(Guid UserId);

public record VoteResponse(Guid Id, Guid NoteId, Guid UserId);
