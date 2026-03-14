using Api5.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api5.IntegrationTests;

/// <summary>
/// Consistency boundary integration tests for API 5 (Behavioral / CQRS / MediatR).
/// Inherits all shared consistency tests from <see cref="ConsistencyBoundaryTestsBase{TFixture}"/>
/// and provides the API 5-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS IN API 5. ✅
///
/// API 5 maintains the consistency guarantees of API 3/4 but with the
/// enforcement logic moved from service methods to MediatR command handlers:
///   - RetroBoard aggregate still enforces column name uniqueness and note
///     text uniqueness via in-memory checks protected by xmin concurrency.
///   - Vote uniqueness ("one vote per user per note") is enforced by:
///     1. Application-level check in <c>CastVoteCommandHandler</c>.
///     2. DB unique constraint on (NoteId, UserId) (safety net).
///   - Cross-aggregate checks (note exists, user is project member) are
///     performed by <c>CastVoteCommandHandler</c> before creating the
///     Vote aggregate — same checks as API 4's <c>VoteService</c>, but
///     living inside a command handler instead.
///
/// The key API 5 insight: the aggregate boundaries and consistency guarantees
/// are architectural decisions that persist across the service → handler
/// transition. CQRS/MediatR changes HOW we dispatch, not WHAT we enforce.
/// </remarks>
[Collection("Api5 Integration Tests")]
public class Api5ConsistencyBoundaryTests : ConsistencyBoundaryTestsBase<Api5Fixture>, IClassFixture<Api5Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 5 fixture.
    /// </summary>
    /// <param name="fixture">The API 5 fixture providing HTTP client and DB reset.</param>
    public Api5ConsistencyBoundaryTests(Api5Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
