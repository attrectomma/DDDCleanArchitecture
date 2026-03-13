using System.Net;
using FluentAssertions;
using RetroBoard.IntegrationTests.Shared.DTOs;
using RetroBoard.IntegrationTests.Shared.Extensions;
using Xunit;

namespace RetroBoard.IntegrationTests.Shared.Tests;

/// <summary>
/// Abstract base class containing soft delete integration tests.
/// Validates that deleted entities are excluded from queries
/// but remain in the database (soft delete via DeletedAt timestamp).
/// </summary>
public abstract class SoftDeleteTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    protected SoftDeleteTestsBase(HttpClient client, Func<Task> resetDatabase)
    {
        _client = client;
        _resetDatabase = resetDatabase;
    }

    public async Task InitializeAsync()
    {
        await _resetDatabase();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DeleteColumn_ThenGetRetro_ColumnNotInResponse()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("SoftDelete Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("SoftDelete Retro"));

        var column1 = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Keep Me"));
        var column2 = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Delete Me"));

        // Act — soft delete the second column
        await _client.DeleteAndExpectNoContentAsync(
            $"/api/retros/{retro.Id}/columns/{column2.Id}");

        // Assert — fetch the retro and verify only the first column remains
        var fetchedRetro = await _client.GetAndExpectOkAsync<RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros/{retro.Id}");

        fetchedRetro.Columns.Should().NotBeNull();
        fetchedRetro.Columns.Should().HaveCount(1);
        fetchedRetro.Columns![0].Name.Should().Be("Keep Me");
    }

    [Fact]
    public async Task DeleteNote_ThenGetColumn_NoteNotInResponse()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("SoftDelete Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("SoftDelete Retro"));
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("My Column"));

        var note1 = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Keep This Note"));
        var note2 = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Delete This Note"));

        // Act — soft delete the second note
        await _client.DeleteAndExpectNoContentAsync(
            $"/api/columns/{column.Id}/notes/{note2.Id}");

        // Assert — fetch the retro and verify only the first note remains
        var fetchedRetro = await _client.GetAndExpectOkAsync<RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros/{retro.Id}");

        var fetchedColumn = fetchedRetro.Columns!.First(c => c.Id == column.Id);
        fetchedColumn.Notes.Should().HaveCount(1);
        fetchedColumn.Notes![0].Text.Should().Be("Keep This Note");
    }

    [Fact]
    public async Task DeleteColumn_ThenRecreateSameName_Succeeds()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("SoftDelete Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("SoftDelete Retro"));

        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Reusable Name"));

        // Delete the column
        await _client.DeleteAndExpectNoContentAsync(
            $"/api/retros/{retro.Id}/columns/{column.Id}");

        // Act — create a new column with the same name (should succeed because original is soft-deleted)
        var newColumn = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Reusable Name"));

        // Assert
        newColumn.Id.Should().NotBe(column.Id);
        newColumn.Name.Should().Be("Reusable Name");
    }
}
