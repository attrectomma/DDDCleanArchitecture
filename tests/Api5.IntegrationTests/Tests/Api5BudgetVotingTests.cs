using System.Net;
using System.Net.Http.Json;
using Api5.IntegrationTests.Fixtures;
using FluentAssertions;
using RetroBoard.IntegrationTests.Shared.DTOs;
using RetroBoard.IntegrationTests.Shared.Extensions;
using Xunit;

namespace Api5.IntegrationTests;

/// <summary>
/// Integration tests for the Budget voting strategy end-to-end flow.
/// These tests are Api5-specific because only API 5 supports configurable
/// voting strategies via the Strategy + Specification patterns.
/// </summary>
/// <remarks>
/// DESIGN: These tests exercise the <c>BudgetVotingStrategy</c> through
/// the full HTTP pipeline: controller → MediatR → command handler →
/// strategy factory → specification validation → persistence.
///
/// The tests verify:
///   - A retro board can be created with the Budget voting strategy.
///   - Budget voting allows multiple votes on the same note (dot voting).
///   - Budget voting enforces the per-column vote limit.
///   - The voting strategy can be changed on an existing board.
///
/// Compare with the shared concurrency tests which run against ALL APIs
/// and assume the Default voting strategy. These tests are unique to API 5
/// and validate the Strategy pattern integration end-to-end.
///
/// DESIGN: These tests use <see cref="Api5BudgetFixture"/> instead of the
/// standard <see cref="Api5Fixture"/>. The Budget fixture configures
/// <c>VotingOptions.DefaultVotingStrategy = Budget</c>, which means the
/// database schema does NOT include a unique index on <c>Vote(NoteId, UserId)</c>.
/// This is necessary because the Budget strategy allows multiple votes per
/// user per note. The separate fixture demonstrates that the Options pattern
/// can influence not just application behaviour but also the database schema.
/// </remarks>
[Collection("Api5 Budget Integration Tests")]
public class Api5BudgetVotingTests : IClassFixture<Api5BudgetFixture>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    /// <summary>
    /// Initializes a new instance of <see cref="Api5BudgetVotingTests"/>.
    /// </summary>
    /// <param name="fixture">The API 5 Budget fixture providing HTTP client and DB reset.</param>
    public Api5BudgetVotingTests(Api5BudgetFixture fixture)
    {
        _client = fixture.Client;
        _resetDatabase = fixture.ResetDatabaseAsync;
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _resetDatabase();
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Creates a retro board with the Budget strategy and verifies the
    /// response includes the correct voting strategy name.
    /// </summary>
    [Fact]
    public async Task CreateRetroBoard_WithBudgetStrategy_ReturnsCorrectStrategy()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Budget Project"));

        // Act — create a retro board with explicit Budget strategy
        // DESIGN: VotingStrategyType.Budget = 1. We send the integer because
        // ASP.NET Core's default System.Text.Json does not deserialize string
        // enum names without a JsonStringEnumConverter. Using integers keeps
        // the API contract simple and avoids custom serialisation config.
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/retros",
            new { Name = "Budget Retro", VotingStrategy = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        Api5RetroBoardResponse? retro = await response.Content.ReadFromJsonAsync<Api5RetroBoardResponse>();
        retro.Should().NotBeNull();
        retro!.VotingStrategy.Should().Be("Budget");
    }

    /// <summary>
    /// Under the Budget strategy, a user can cast multiple votes on the
    /// same note (dot voting). This is the key behavioural difference from
    /// the Default strategy.
    /// </summary>
    [Fact]
    public async Task CastVote_BudgetStrategy_AllowsMultipleVotesOnSameNote()
    {
        // Arrange
        var (user, note) = await SetUpBudgetBoardWithNoteAsync();

        // Act — cast two votes on the same note by the same user
        HttpResponseMessage firstVote = await _client.PostAsJsonAsync(
            $"/api/notes/{note.Id}/votes", new CastVoteRequest(user.Id));
        HttpResponseMessage secondVote = await _client.PostAsJsonAsync(
            $"/api/notes/{note.Id}/votes", new CastVoteRequest(user.Id));

        // Assert — both should succeed (Budget allows duplicates)
        firstVote.StatusCode.Should().Be(HttpStatusCode.Created);
        secondVote.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    /// <summary>
    /// Under the Budget strategy, a user cannot exceed the per-column vote
    /// limit (default: 3). The fourth vote on notes within the same column
    /// should be rejected.
    /// </summary>
    [Fact]
    public async Task CastVote_BudgetStrategy_ExceedingBudget_ReturnsUnprocessableEntity()
    {
        // Arrange — create a board with Budget strategy, a column with 3 notes
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("BudgetVoter", "budget@example.com"));
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Budget Limit Project"));

        // Create retro with Budget strategy (VotingStrategyType.Budget = 1)
        HttpResponseMessage retroResponse = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/retros",
            new { Name = "Budget Limit Retro", VotingStrategy = 1 });
        Api5RetroBoardResponse retro = (await retroResponse.Content.ReadFromJsonAsync<Api5RetroBoardResponse>())!;

        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Votes Column"));

        var note1 = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Note 1"));
        var note2 = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Note 2"));
        var note3 = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Note 3"));
        var note4 = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Note 4"));

        // Act — cast 3 votes (should succeed) then a 4th (should fail)
        HttpResponseMessage vote1 = await _client.PostAsJsonAsync(
            $"/api/notes/{note1.Id}/votes", new CastVoteRequest(user.Id));
        HttpResponseMessage vote2 = await _client.PostAsJsonAsync(
            $"/api/notes/{note2.Id}/votes", new CastVoteRequest(user.Id));
        HttpResponseMessage vote3 = await _client.PostAsJsonAsync(
            $"/api/notes/{note3.Id}/votes", new CastVoteRequest(user.Id));
        HttpResponseMessage vote4 = await _client.PostAsJsonAsync(
            $"/api/notes/{note4.Id}/votes", new CastVoteRequest(user.Id));

        // Assert — first 3 succeed, 4th is rejected (budget exceeded)
        // DESIGN: The BudgetVotingStrategy throws InvariantViolationException
        // when the per-column budget is exceeded. The GlobalExceptionHandlerMiddleware
        // maps InvariantViolationException → 409 Conflict.
        vote1.StatusCode.Should().Be(HttpStatusCode.Created);
        vote2.StatusCode.Should().Be(HttpStatusCode.Created);
        vote3.StatusCode.Should().Be(HttpStatusCode.Created);
        vote4.StatusCode.Should().Be(
            HttpStatusCode.Conflict,
            "the 4th vote should be rejected because the per-column budget (3) is exceeded");
    }

    /// <summary>
    /// Changing the voting strategy on an existing retro board should
    /// return the updated strategy in the response.
    /// </summary>
    /// <remarks>
    /// DESIGN: This test verifies the ChangeVotingStrategy endpoint. The Budget
    /// fixture defaults to Budget, so a board created without explicit strategy
    /// uses Budget. Changing it to Default should return "Default" in the response.
    /// </remarks>
    [Fact]
    public async Task ChangeVotingStrategy_FromBudgetToDefault_ReturnsUpdatedStrategy()
    {
        // Arrange — create a retro board (defaults to Budget in this fixture)
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Strategy Change Project"));

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/retros",
            new { Name = "Strategy Change Retro" });
        Api5RetroBoardResponse retro = (await createResponse.Content.ReadFromJsonAsync<Api5RetroBoardResponse>())!;
        retro.VotingStrategy.Should().Be("Budget");

        // Act — change to Default strategy (VotingStrategyType.Default = 0)
        HttpResponseMessage changeResponse = await _client.PutAsJsonAsync(
            $"/api/retros/{retro.Id}/voting-strategy",
            new { VotingStrategy = 0 });

        // Assert
        changeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Api5RetroBoardResponse? updated = await changeResponse.Content.ReadFromJsonAsync<Api5RetroBoardResponse>();
        updated.Should().NotBeNull();
        updated!.VotingStrategy.Should().Be("Default");
    }

    /// <summary>
    /// Helper that sets up a Budget-strategy retro board with a single note
    /// and a user, ready for voting tests.
    /// </summary>
    private async Task<(UserResponse User, NoteResponse Note)> SetUpBudgetBoardWithNoteAsync()
    {
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("DotVoter", "dot@example.com"));
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Dot Voting Project"));

        // Create retro with Budget strategy (VotingStrategyType.Budget = 1)
        HttpResponseMessage retroResponse = await _client.PostAsJsonAsync(
            $"/api/projects/{project.Id}/retros",
            new { Name = "Dot Voting Retro", VotingStrategy = 1 });
        Api5RetroBoardResponse retro = (await retroResponse.Content.ReadFromJsonAsync<Api5RetroBoardResponse>())!;

        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Dot Column"));
        var note = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Dot Note"));

        return (user, note);
    }

    /// <summary>
    /// Api5-specific retro board response DTO that includes the VotingStrategy field.
    /// The shared DTO does not include this field because it is Api5-only.
    /// </summary>
    private record Api5RetroBoardResponse(
        Guid Id,
        string Name,
        Guid ProjectId,
        DateTime CreatedAt,
        List<ColumnResponse>? Columns,
        string VotingStrategy);
}
