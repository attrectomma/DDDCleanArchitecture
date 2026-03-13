using Api1.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api1.IntegrationTests;

/// <summary>
/// Consistency boundary integration tests for API 1 (Anemic CRUD).
/// Inherits all shared consistency tests from <see cref="ConsistencyBoundaryTestsBase{TFixture}"/>
/// and provides the API 1-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ⚠️ SOME OF THESE TESTS ARE EXPECTED TO FAIL IN API 1. ⚠️
///
/// This class contains two categories of tests:
///
/// ✅ Baseline tests (pass):
///   - <c>AddNoteToDeletedColumn_IsRejected</c> — EF Core global query filter handles this
///   - <c>UpdateColumn_ToMatchExistingName_IsRejected</c> — service-layer check works sequentially
///
/// ❌ Cross-entity boundary tests (fail):
///   - <c>CastVote_ByNonProjectMember_IsRejected</c> — VoteService never checks project membership
///     because it only validates its own narrow entity scope (Note, User, Vote). The full chain
///     User → ProjectMember → Project → RetroBoard → Column → Note is never traversed.
///   - <c>CastVote_OnNoteInDeletedColumn_IsRejected</c> — VoteService queries the note directly,
///     finds it (Note.DeletedAt is null), and allows the vote. It has no awareness that the note's
///     parent column was soft-deleted. Each entity is an island.
///
/// These failures demonstrate the core problem of the anemic CRUD architecture:
/// without aggregate boundaries, each service only validates its own slice of the domain.
/// Cross-entity invariants (membership, parent lifecycle) simply cannot be enforced because
/// no single component "owns" the full consistency boundary.
///
/// Do NOT "fix" these by adding more checks to VoteService. The architectural solution is
/// aggregate roots (API 3+), which load the entire boundary and enforce all rules within it.
/// </remarks>
[Collection("Api1 Integration Tests")]
public class Api1ConsistencyBoundaryTests : ConsistencyBoundaryTestsBase<Api1Fixture>, IClassFixture<Api1Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 1 fixture.
    /// </summary>
    /// <param name="fixture">The API 1 fixture providing HTTP client and DB reset.</param>
    public Api1ConsistencyBoundaryTests(Api1Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
