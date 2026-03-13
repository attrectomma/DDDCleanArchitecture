using FluentAssertions;
using RetroBoard.IntegrationTests.Shared.DTOs;
using RetroBoard.IntegrationTests.Shared.Extensions;
using Xunit;

namespace RetroBoard.IntegrationTests.Shared.Tests;

/// <summary>
/// Abstract base class containing CRUD integration tests that all five
/// API test projects inherit. Each API-specific test class provides the
/// <see cref="ApiFixture{TProgram}"/> via its constructor.
/// </summary>
/// <remarks>
/// DESIGN: By making these tests abstract, we write them once and run them
/// against every API. If all five APIs expose the same REST contract, the
/// same test code validates all of them. API-specific test projects inherit
/// this class and only provide the fixture wiring.
///
/// Tests are organized into regions matching the domain concepts.
/// Each test method resets the database before running to ensure isolation.
/// </remarks>
/// <typeparam name="TFixture">
/// The API-specific fixture type (e.g., Api1Fixture).
/// </typeparam>
public abstract class CrudTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    /// <summary>
    /// Initializes the base test class with an HTTP client and a
    /// database reset function from the API fixture.
    /// </summary>
    /// <param name="client">HTTP client configured for the API under test.</param>
    /// <param name="resetDatabase">Function to reset the database between tests.</param>
    protected CrudTestsBase(HttpClient client, Func<Task> resetDatabase)
    {
        _client = client;
        _resetDatabase = resetDatabase;
    }

    /// <summary>
    /// Resets the database before each test for isolation.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _resetDatabase();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── User Tests ──────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_WithValidData_Returns201AndUser()
    {
        // Arrange
        var request = new CreateUserRequest("Alice", "alice@example.com");

        // Act
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", request);

        // Assert
        user.Id.Should().NotBeEmpty();
        user.Name.Should().Be("Alice");
        user.Email.Should().Be("alice@example.com");
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetUser_AfterCreation_ReturnsUser()
    {
        // Arrange
        var created = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("Bob", "bob@example.com"));

        // Act
        var fetched = await _client.GetAndExpectOkAsync<UserResponse>(
            $"/api/users/{created.Id}");

        // Assert
        fetched.Id.Should().Be(created.Id);
        fetched.Name.Should().Be("Bob");
    }

    // ── Project Tests ───────────────────────────────────────────

    [Fact]
    public async Task CreateProject_WithValidData_Returns201()
    {
        // Arrange & Act
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Sprint 42 Retro"));

        // Assert
        project.Id.Should().NotBeEmpty();
        project.Name.Should().Be("Sprint 42 Retro");
    }

    [Fact]
    public async Task AddMember_WithValidUser_Returns201()
    {
        // Arrange
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("Charlie", "charlie@example.com"));
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("My Project"));

        // Act & Assert — should not throw
        await _client.PostAndExpectCreatedAsync<AddMemberRequest, object>(
            $"/api/projects/{project.Id}/members", new AddMemberRequest(user.Id));
    }

    // ── RetroBoard Tests ────────────────────────────────────────

    [Fact]
    public async Task CreateRetroBoard_WithValidData_Returns201()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("My Project"));

        // Act
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Sprint 42"));

        // Assert
        retro.Id.Should().NotBeEmpty();
        retro.Name.Should().Be("Sprint 42");
        retro.ProjectId.Should().Be(project.Id);
    }

    // ── Column Tests ────────────────────────────────────────────

    [Fact]
    public async Task AddColumn_WithValidData_Returns201()
    {
        // Arrange
        var (_, retroId) = await CreateProjectAndRetro();

        // Act
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retroId}/columns", new CreateColumnRequest("What went well"));

        // Assert
        column.Id.Should().NotBeEmpty();
        column.Name.Should().Be("What went well");
    }

    [Fact]
    public async Task DeleteColumn_AfterCreation_Returns204()
    {
        // Arrange
        var (_, retroId) = await CreateProjectAndRetro();
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retroId}/columns", new CreateColumnRequest("To Remove"));

        // Act & Assert
        await _client.DeleteAndExpectNoContentAsync(
            $"/api/retros/{retroId}/columns/{column.Id}");
    }

    // ── Note Tests ──────────────────────────────────────────────

    [Fact]
    public async Task AddNote_WithValidData_Returns201()
    {
        // Arrange
        var columnId = await CreateProjectRetroAndColumn();

        // Act
        var note = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{columnId}/notes", new CreateNoteRequest("Great teamwork!"));

        // Assert
        note.Id.Should().NotBeEmpty();
        note.Text.Should().Be("Great teamwork!");
    }

    // ── Vote Tests ──────────────────────────────────────────────

    [Fact]
    public async Task CastVote_WithValidUser_Returns201()
    {
        // Arrange
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("Voter", "voter@example.com"));
        var columnId = await CreateProjectRetroAndColumn();
        var note = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{columnId}/notes", new CreateNoteRequest("Vote on me"));

        // Act
        var vote = await _client.PostAndExpectCreatedAsync<CastVoteRequest, VoteResponse>(
            $"/api/notes/{note.Id}/votes", new CastVoteRequest(user.Id));

        // Assert
        vote.Id.Should().NotBeEmpty();
        vote.NoteId.Should().Be(note.Id);
        vote.UserId.Should().Be(user.Id);
    }

    // ── Helper Methods ──────────────────────────────────────────

    /// <summary>
    /// Creates a project and retro board for use in tests.
    /// Returns (projectId, retroBoardId).
    /// </summary>
    protected async Task<(Guid ProjectId, Guid RetroId)> CreateProjectAndRetro()
    {
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Test Project"));

        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Test Retro"));

        return (project.Id, retro.Id);
    }

    /// <summary>
    /// Creates a project, retro board, and column for use in tests.
    /// Returns the column ID.
    /// </summary>
    protected async Task<Guid> CreateProjectRetroAndColumn()
    {
        var (_, retroId) = await CreateProjectAndRetro();

        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retroId}/columns", new CreateColumnRequest("Test Column"));

        return column.Id;
    }
}
