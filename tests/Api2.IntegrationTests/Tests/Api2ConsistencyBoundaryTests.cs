using Api2.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api2.IntegrationTests;

/// <summary>
/// Consistency boundary integration tests for API 2 (Rich Domain).
/// Inherits all shared consistency tests from <see cref="ConsistencyBoundaryTestsBase{TFixture}"/>
/// and provides the API 2-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ⚠️ SOME OF THESE TESTS ARE EXPECTED TO FAIL IN API 2. ⚠️
///
/// This class contains two categories of tests:
///
/// ✅ Baseline tests (pass):
///   - <c>AddNoteToDeletedColumn_IsRejected</c> — EF Core global query filter handles this
///   - <c>UpdateColumn_ToMatchExistingName_IsRejected</c> — service-layer check works sequentially
///
/// ❌ Cross-entity boundary tests (fail):
///   - <c>CastVote_ByNonProjectMember_IsRejected</c> — VoteService never checks project membership
///     because it only validates its own narrow entity scope (Note, User). The full chain
///     User → ProjectMember → Project → RetroBoard → Column → Note is never traversed.
///   - <c>CastVote_OnNoteInDeletedColumn_IsRejected</c> — VoteService loads the note directly,
///     finds it (Note.DeletedAt is null), and allows the vote. It has no awareness that the note's
///     parent column was soft-deleted.
///
/// Rich domain entities are better than anemic ones, but without aggregate boundaries
/// each entity still only enforces its own local invariants. Cross-entity consistency
/// requires an aggregate root (API 3+) that loads and validates the full boundary.
/// </remarks>
[Collection("Api2 Integration Tests")]
public class Api2ConsistencyBoundaryTests : ConsistencyBoundaryTestsBase<Api2Fixture>, IClassFixture<Api2Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 2 fixture.
    /// </summary>
    /// <param name="fixture">The API 2 fixture providing HTTP client and DB reset.</param>
    public Api2ConsistencyBoundaryTests(Api2Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
