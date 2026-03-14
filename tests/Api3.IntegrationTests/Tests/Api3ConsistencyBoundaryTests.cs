using Api3.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api3.IntegrationTests;

/// <summary>
/// Consistency boundary integration tests for API 3 (Aggregate Design).
/// Inherits all shared consistency tests from <see cref="ConsistencyBoundaryTestsBase{TFixture}"/>
/// and provides the API 3-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS IN API 3. ✅
///
/// API 3 introduces aggregate boundaries that enforce cross-entity consistency:
///
/// ✅ All tests pass because:
///   - The RetroBoard aggregate root loads the complete graph (columns, notes, votes).
///   - All operations go through the aggregate root, which validates the full state.
///   - Soft-deleted child entities are filtered by EF Core's global query filters
///     and excluded from the in-memory aggregate state.
///   - Cross-entity checks (e.g., voting on a note in a deleted column) are caught
///     because the aggregate root loads the full boundary and validates it.
///
/// This is a major improvement over API 2, where VoteService had no awareness
/// of the parent column's deletion state or the user's project membership.
/// </remarks>
[Collection("Api3 Integration Tests")]
public class Api3ConsistencyBoundaryTests : ConsistencyBoundaryTestsBase<Api3Fixture>, IClassFixture<Api3Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 3 fixture.
    /// </summary>
    /// <param name="fixture">The API 3 fixture providing HTTP client and DB reset.</param>
    public Api3ConsistencyBoundaryTests(Api3Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
