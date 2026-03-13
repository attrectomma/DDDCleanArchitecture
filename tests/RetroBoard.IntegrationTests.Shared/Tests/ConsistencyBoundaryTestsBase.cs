using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RetroBoard.IntegrationTests.Shared.DTOs;
using RetroBoard.IntegrationTests.Shared.Extensions;
using Xunit;

namespace RetroBoard.IntegrationTests.Shared.Tests;

/// <summary>
/// Abstract base class containing consistency boundary tests.
/// These tests validate that the API correctly enforces business invariants
/// that span multiple entities — the kind of rules that require an aggregate
/// root to coordinate.
/// </summary>
/// <remarks>
/// DESIGN: This class contains two categories of tests:
///
/// 1. **Baseline boundary tests** (pass on all APIs):
///    - <see cref="AddNoteToDeletedColumn_IsRejected"/>
///    - <see cref="UpdateColumn_ToMatchExistingName_IsRejected"/>
///    These work even in API 1/2 because EF Core's global query filter or the
///    service-layer check handles the single-request case adequately.
///
/// 2. **Cross-entity boundary tests** (❌ FAIL on API 1/2, ✅ PASS on API 3+):
///    - <see cref="CastVote_ByNonProjectMember_IsRejected"/>
///    - <see cref="CastVote_OnNoteInDeletedColumn_IsRejected"/>
///    These expose the fact that without aggregate boundaries, services only
///    validate their own immediate entity — they never check the broader context
///    (project membership, parent lifecycle). Aggregates solve this because the
///    root loads the entire consistency boundary and validates all rules.
///
/// Unlike <see cref="InvariantTestsBase{TFixture}"/> (which tests single-entity
/// invariants like "no duplicate column names") and <see cref="ConcurrencyTestsBase{TFixture}"/>
/// (which tests parallel race conditions), this class tests whether the API
/// understands which entities form a transactional unit.
/// </remarks>
public abstract class ConsistencyBoundaryTestsBase<TFixture> : IAsyncLifetime
    where TFixture : class
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    protected ConsistencyBoundaryTestsBase(HttpClient client, Func<Task> resetDatabase)
    {
        _client = client;
        _resetDatabase = resetDatabase;
    }

    public async Task InitializeAsync()
    {
        await _resetDatabase();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Baseline Boundary Tests (pass on all APIs) ──────────────

    /// <summary>
    /// Tests that soft-deleting a column prevents adding new notes to it,
    /// validating that the soft-delete boundary is respected.
    /// </summary>
    /// <remarks>
    /// DESIGN: This passes even in API 1/2 because <c>NoteService</c> queries
    /// for the column via a repository that has a global query filter
    /// (<c>DeletedAt == null</c>). The soft-deleted column is invisible,
    /// so the service throws <c>NotFoundException</c>.
    ///
    /// In API 3+, the aggregate root itself would reject the operation
    /// via a domain method — a stronger guarantee, but the observable
    /// HTTP behaviour is the same.
    /// </remarks>
    [Fact]
    public async Task AddNoteToDeletedColumn_IsRejected()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Delete Boundary Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Delete Boundary Retro"));
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Column to Delete"));

        // Act — delete the column, then attempt to add a note
        var deleteResponse = await _client.DeleteAsync($"/api/retros/{retro.Id}/columns/{column.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var addNoteResponse = await _client.PostAsJsonAsync(
            $"/api/columns/{column.Id}/notes",
            new CreateNoteRequest("This should fail"));

        // Assert
        addNoteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "cannot add a note to a soft-deleted column");
    }

    /// <summary>
    /// Tests that updating a column's name to match another column in the same
    /// retro board is correctly rejected.
    /// </summary>
    /// <remarks>
    /// DESIGN: This passes in API 1/2 because the service-layer uniqueness check
    /// works for sequential (non-concurrent) requests. The service queries
    /// "does another column with this name exist?" and rejects if so.
    ///
    /// In API 3+, the RetroBoard aggregate enforces uniqueness transactionally,
    /// which also handles the concurrent case (tested in
    /// <see cref="ConcurrencyTestsBase{TFixture}"/>).
    /// </remarks>
    [Fact]
    public async Task UpdateColumn_ToMatchExistingName_IsRejected()
    {
        // Arrange
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Update Boundary Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Update Boundary Retro"));

        var column1 = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Column One"));
        var column2 = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Column Two"));

        // Act — try to rename Column Two to "Column One"
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/retros/{retro.Id}/columns/{column2.Id}",
            new UpdateColumnRequest("Column One"));

        // Assert
        updateResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Conflict,
            HttpStatusCode.UnprocessableEntity);
    }

    // ── Cross-Entity Boundary Tests (FAIL on API 1/2) ───────────

    /// <summary>
    /// Tests that a user who is NOT a member of the project cannot vote
    /// on notes within that project's retro boards.
    /// </summary>
    /// <remarks>
    /// DESIGN: Domain invariant #4 states "Only Users assigned to the Project
    /// may participate in its Retros." This is a cross-entity invariant that
    /// spans User → ProjectMember → Project → RetroBoard → Column → Note → Vote.
    ///
    ///   API 1/2: ❌ FAILS — <c>VoteService</c> only checks three things:
    ///            (1) note exists, (2) user exists, (3) user hasn't already voted.
    ///            It never walks up the entity chain to verify that the user is a
    ///            member of the project that owns the retro board. The vote is
    ///            created successfully for a non-member. This is the hallmark
    ///            problem of missing aggregate boundaries — each service only
    ///            validates its own narrow slice of the domain.
    ///
    ///   API 3+:  ✅ PASSES — The application service (or command handler in API 5)
    ///            loads the aggregate root and checks project membership before
    ///            allowing the operation. The aggregate defines the consistency
    ///            boundary, so all cross-entity rules within that boundary are
    ///            enforced transactionally.
    ///
    /// This test does NOT need new endpoints — it uses existing endpoints in a
    /// specific sequence that exposes the boundary gap.
    /// </remarks>
    [Fact]
    public async Task CastVote_ByNonProjectMember_IsRejected()
    {
        // Arrange — create a member and a non-member
        var member = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("ProjectMember", "member@example.com"));
        var outsider = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("Outsider", "outsider@example.com"));

        // Create a project and add only the member
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Members-Only Project"));
        await _client.PostAndExpectCreatedAsync<AddMemberRequest, object>(
            $"/api/projects/{project.Id}/members", new AddMemberRequest(member.Id));

        // Create the full retro structure
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Members Retro"));
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Members Column"));
        var note = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Members Note"));

        // Act — the outsider (non-member) tries to vote
        var voteResponse = await _client.PostAsJsonAsync(
            $"/api/notes/{note.Id}/votes",
            new CastVoteRequest(outsider.Id));

        // Assert — should be rejected because outsider is not a project member
        voteResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.Forbidden,
            HttpStatusCode.Conflict,
            HttpStatusCode.UnprocessableEntity);
    }

    /// <summary>
    /// Tests that voting on a note whose parent column has been soft-deleted
    /// is correctly rejected — even though the note itself is not deleted.
    /// </summary>
    /// <remarks>
    /// DESIGN: This exposes a "parent lifecycle leak" — a fundamental gap in
    /// architectures without aggregate boundaries.
    ///
    ///   API 1/2: ❌ FAILS — <c>VoteService</c> queries the note directly via
    ///            <c>_noteRepository.GetByIdAsync(noteId)</c>. The note's own
    ///            <c>DeletedAt</c> is null (only the column was deleted), so the
    ///            note is found and the vote succeeds. The service has no awareness
    ///            that the note's column (its logical parent) has been deleted.
    ///            Each entity is an island — there is no "boundary" that would
    ///            cascade the deletion semantics to children.
    ///
    ///   API 3+:  ✅ PASSES — The RetroBoard aggregate root loads all its children
    ///            (columns, notes, votes). The soft-deleted column is excluded by
    ///            the EF Core query filter during aggregate hydration, so its notes
    ///            are unreachable. Any attempt to operate on them fails because the
    ///            aggregate root cannot find the note within its boundary.
    ///
    /// This is distinct from <see cref="AddNoteToDeletedColumn_IsRejected"/> which
    /// passes in all APIs because the POST /columns/{id}/notes endpoint explicitly
    /// looks up the column (hidden by query filter). Here, the vote endpoint only
    /// looks up the note — which is NOT deleted — so the query filter doesn't help.
    /// </remarks>
    [Fact]
    public async Task CastVote_OnNoteInDeletedColumn_IsRejected()
    {
        // Arrange — create the full entity chain
        var user = await _client.PostAndExpectCreatedAsync<CreateUserRequest, UserResponse>(
            "/api/users", new CreateUserRequest("GhostVoter", "ghost@example.com"));
        var project = await _client.PostAndExpectCreatedAsync<CreateProjectRequest, ProjectResponse>(
            "/api/projects", new CreateProjectRequest("Ghost Project"));
        var retro = await _client.PostAndExpectCreatedAsync<CreateRetroBoardRequest, RetroBoardResponse>(
            $"/api/projects/{project.Id}/retros", new CreateRetroBoardRequest("Ghost Retro"));
        var column = await _client.PostAndExpectCreatedAsync<CreateColumnRequest, ColumnResponse>(
            $"/api/retros/{retro.Id}/columns", new CreateColumnRequest("Doomed Column"));
        var note = await _client.PostAndExpectCreatedAsync<CreateNoteRequest, NoteResponse>(
            $"/api/columns/{column.Id}/notes", new CreateNoteRequest("Orphaned Note"));

        // Act — soft-delete the column, then try to vote on the orphaned note
        await _client.DeleteAndExpectNoContentAsync(
            $"/api/retros/{retro.Id}/columns/{column.Id}");

        var voteResponse = await _client.PostAsJsonAsync(
            $"/api/notes/{note.Id}/votes",
            new CastVoteRequest(user.Id));

        // Assert — should be rejected because the note's column is deleted
        voteResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.Conflict,
            HttpStatusCode.UnprocessableEntity);
    }
}
