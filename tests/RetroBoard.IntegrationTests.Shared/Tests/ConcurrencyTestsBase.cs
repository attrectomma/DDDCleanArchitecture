using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RetroBoard.IntegrationTests.Shared.DTOs;
using RetroBoard.IntegrationTests.Shared.Extensions;
using Xunit;

namespace RetroBoard.IntegrationTests.Shared.Tests;

/// <summary>
/// Abstract base class containing concurrency tests.
/// These tests validate that the API correctly handles simultaneous writes
/// to the same aggregate/resource.
/// </summary>
/// <remarks>
/// DESIGN: These tests are expected to:
///   - ❌ FAIL on API 1 and API 2 (no consistency boundary, no optimistic concurrency)
///   - ✅ PASS on API 3, API 4, and API 5 (aggregate boundaries + concurrency tokens)
///
/// The tests simulate concurrent operations by firing multiple requests
/// in parallel. In APIs without proper aggregate locking, race conditions
/// allow invariant violations. In APIs with proper design, one of the
/// concurrent requests will receive a 409 Conflict.
/// </remarks>
public abstract class ConcurrencyTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    protected ConcurrencyTestsBase(HttpClient client, Func<Task> resetDatabase)
    {
        _client = client;
        _resetDatabase = resetDatabase;
    }

    public async Task InitializeAsync()
    {
        await _resetDatabase();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Fires two identical "add column" requests concurrently with the same name.
    /// Without aggregate-level locking, both requests pass the uniqueness check
    /// and create duplicate columns. With proper aggregate design, one request
    /// should fail with 409 Conflict.
    /// </summary>
    /// <remarks>
    /// DESIGN: This is the classic "check-then-act" race condition.
    ///   API 1/2: Both requests check "does column X exist?" → both get false →
    ///            both create column X → duplicate! Test expects failure here.
    ///   API 3+:  Optimistic concurrency on the aggregate root means the second
    ///            SaveChanges throws DbUpdateConcurrencyException → 409 Conflict.
    /// </remarks>
    [Fact]
    public async Task AddColumn_ConcurrentDuplicateNames_OnlyOneSucceeds()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Concurrency Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Concurrency Retro"));

        var url = $"/api/retros/{retro.Id}/columns";
        var request = new CreateColumnRequest("Concurrent Column");

        // Act — fire two requests simultaneously
        var tasks = Enumerable.Range(0, 2)
            .Select(_ => _client.PostAsJsonAsync(url, request))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert — exactly one should succeed (201), the other should fail (409 or 422)
        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.Created,
            "exactly one request should succeed");
        statusCodes.Should().ContainSingle(s =>
            s == HttpStatusCode.Conflict || s == HttpStatusCode.UnprocessableEntity,
            "the other request should be rejected due to the uniqueness constraint");
    }

    /// <summary>
    /// Fires two identical "cast vote" requests concurrently for the same
    /// user on the same note. Without proper enforcement, both votes are created.
    /// </summary>
    [Fact]
    public async Task CastVote_ConcurrentDuplicateVotes_OnlyOneSucceeds()
    {
        // Arrange
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("ConcurrentVoter", "cv@example.com"));
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Vote Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Vote Retro"));
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Vote Column"));
        var note = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Vote Target"));

        var url = $"/api/notes/{note.Id}/votes";
        var voteRequest = new CastVoteRequest(user.Id);

        // Act — fire two vote requests simultaneously
        var tasks = Enumerable.Range(0, 2)
            .Select(_ => _client.PostAsJsonAsync(url, voteRequest))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert — exactly one should succeed
        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        statusCodes.Should().ContainSingle(s => s == HttpStatusCode.Created,
            "exactly one vote should succeed");
        statusCodes.Should().ContainSingle(s =>
            s == HttpStatusCode.Conflict || s == HttpStatusCode.UnprocessableEntity,
            "the duplicate vote should be rejected");
    }
}
