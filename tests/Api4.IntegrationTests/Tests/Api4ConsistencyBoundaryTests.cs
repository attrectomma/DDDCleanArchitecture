using Api4.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api4.IntegrationTests;

/// <summary>
/// Consistency boundary integration tests for API 4 (Split Aggregates).
/// Inherits all shared consistency tests from <see cref="ConsistencyBoundaryTestsBase{TFixture}"/>
/// and provides the API 4-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS IN API 4. ✅
///
/// API 4 maintains the consistency guarantees of API 3 but with a different
/// enforcement mechanism for vote-related invariants:
///   - RetroBoard aggregate still enforces column name uniqueness and note
///     text uniqueness via in-memory checks protected by xmin concurrency.
///   - Vote uniqueness ("one vote per user per note") is now enforced by:
///     1. Application-level check in VoteService (best-effort).
///     2. DB unique constraint on (NoteId, UserId) (ultimate safety net).
///   - Cross-aggregate checks (note exists, user is project member) are
///     performed by VoteService before creating the Vote aggregate.
/// </remarks>
[Collection("Api4 Integration Tests")]
public class Api4ConsistencyBoundaryTests : ConsistencyBoundaryTestsBase<Api4Fixture>, IClassFixture<Api4Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 4 fixture.
    /// </summary>
    /// <param name="fixture">The API 4 fixture providing HTTP client and DB reset.</param>
    public Api4ConsistencyBoundaryTests(Api4Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
