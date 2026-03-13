using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RetroBoard.IntegrationTests.Shared.DTOs;
using RetroBoard.IntegrationTests.Shared.Extensions;
using Xunit;

namespace RetroBoard.IntegrationTests.Shared.Tests;

/// <summary>
/// Abstract base class containing invariant enforcement tests.
/// Validates that business rules (unique names, one-vote-per-user, etc.)
/// are correctly enforced by each API.
/// </summary>
/// <remarks>
/// DESIGN: These tests should pass for ALL five APIs, because even API 1
/// enforces invariants (just not safely under concurrency). The difference
/// between tiers is not WHETHER invariants are checked, but HOW SAFELY
/// they are checked under concurrent access — that's tested in
/// <see cref="ConcurrencyTestsBase{TFixture}"/>.
/// </remarks>
public abstract class InvariantTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    protected InvariantTestsBase(HttpClient client, Func<Task> resetDatabase)
    {
        _client = client;
        _resetDatabase = resetDatabase;
    }

    public async Task InitializeAsync()
    {
        await _resetDatabase();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Column Name Uniqueness ──────────────────────────────────

    [Fact]
    public async Task AddColumn_WithDuplicateName_ReturnsConflictOrError()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Test Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Test Retro"));

        await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("What went well"));

        // Act — attempt to create a column with the same name
        var response = await _client.PostAsJsonAsync(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("What went well"));

        // Assert — should be rejected (409 Conflict or 422 Unprocessable)
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict,
            HttpStatusCode.UnprocessableEntity);
    }

    // ── Note Text Uniqueness ────────────────────────────────────

    [Fact]
    public async Task AddNote_WithDuplicateText_ReturnsConflictOrError()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Test Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Test Retro"));
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Column 1"));

        await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Great job!"));

        // Act — attempt to create a note with the same text
        var response = await _client.PostAsJsonAsync(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Great job!"));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict,
            HttpStatusCode.UnprocessableEntity);
    }

    // ── One Vote Per User Per Note ──────────────────────────────

    [Fact]
    public async Task CastVote_SameUserTwice_ReturnsConflictOrError()
    {
        // Arrange
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("Voter", "voter@example.com"));
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Test Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Test Retro"));
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Column 1"));
        var note = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Vote on me"));

        // First vote — should succeed
        await _client.PostAndExpectCreatedAsync<CastVoteRequest, VoteResponse>(
            $"/api/notes/{note.Id}/votes", new CastVoteRequest(user.Id));

        // Act — second vote by same user on same note
        var response = await _client.PostAsJsonAsync(
            $"/api/notes/{note.Id}/votes", new CastVoteRequest(user.Id));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict,
            HttpStatusCode.UnprocessableEntity);
    }

    // ── Duplicate Project Member ────────────────────────────────

    [Fact]
    public async Task AddMember_SameUserTwice_ReturnsConflictOrError()
    {
        // Arrange
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("Member", "member@example.com"));
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Test Project"));

        await _client.PostAndExpectCreatedAsync<AddMemberRequest, object>(
            $"/api/projects/{project.Id}/members", new AddMemberRequest(user.Id));

        // Act — attempt to add the same user again
        var response = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/members", new AddMemberRequest(user.Id));

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict,
            HttpStatusCode.UnprocessableEntity);
    }
}
