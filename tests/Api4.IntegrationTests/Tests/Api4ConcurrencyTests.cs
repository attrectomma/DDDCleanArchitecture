using Api4.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api4.IntegrationTests;

/// <summary>
/// Concurrency integration tests for API 4 (Split Aggregates).
/// Inherits all shared concurrency tests from <see cref="ConcurrencyTestsBase{TFixture}"/>
/// and provides the API 4-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS IN API 4. ✅
///
/// API 4 maintains the optimistic concurrency from API 3 via xmin tokens
/// on aggregate roots. The key improvement is that Vote is now its own
/// aggregate, so:
///   - Two users voting on different notes NO LONGER conflict (they did in API 3).
///   - Column/note operations on the same retro board still conflict (same as API 3).
///   - Vote operations only conflict if they target the exact same vote row.
///
/// This is the primary motivation for splitting the Vote aggregate:
/// reduced write contention on the RetroBoard aggregate.
/// </remarks>
[Collection("Api4 Integration Tests")]
public class Api4ConcurrencyTests : ConcurrencyTestsBase<Api4Fixture>, IClassFixture<Api4Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 4 fixture.
    /// </summary>
    /// <param name="fixture">The API 4 fixture providing HTTP client and DB reset.</param>
    public Api4ConcurrencyTests(Api4Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
