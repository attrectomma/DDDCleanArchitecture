using Api0b.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0b.IntegrationTests;

/// <summary>
/// Consistency boundary integration tests for Api0b (Transaction Script + Concurrency Safety).
/// Inherits all shared consistency boundary tests from
/// <see cref="ConsistencyBoundaryTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: Mixed results expected — same as Api0a.
///   - Baseline boundary tests (soft-delete visibility, name uniqueness) ✅ PASS
///   - Cross-entity boundary tests (project membership checks, deleted-parent checks) ❌ FAIL
///
/// The concurrency safety changes in Api0b (xmin + unique constraint handling)
/// do NOT add aggregate boundaries. Cross-entity invariants still aren't
/// enforced because endpoint handlers only validate their immediate entity.
/// Fixing this would require the aggregate pattern (API 3+) — which is
/// beyond the Transaction Script approach.
/// </remarks>
[Collection("Api0b Integration Tests")]
public class Api0bConsistencyBoundaryTests : ConsistencyBoundaryTestsBase<Api0bFixture>, IClassFixture<Api0bFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0b fixture.
    /// </summary>
    /// <param name="fixture">The Api0b fixture providing HTTP client and DB reset.</param>
    public Api0bConsistencyBoundaryTests(Api0bFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
