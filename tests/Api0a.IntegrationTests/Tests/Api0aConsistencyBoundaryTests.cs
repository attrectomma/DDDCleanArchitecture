using Api0a.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0a.IntegrationTests;

/// <summary>
/// Consistency boundary integration tests for Api0a (Transaction Script).
/// Inherits all shared consistency boundary tests from
/// <see cref="ConsistencyBoundaryTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: Mixed results expected — same as API 1/2.
///   - Baseline boundary tests (soft-delete visibility, name uniqueness) ✅ PASS
///   - Cross-entity boundary tests (project membership checks, deleted-parent checks) ❌ FAIL
///
/// The Transaction Script pattern has no aggregate boundary concept.
/// Cross-entity invariants are not enforced because endpoint handlers
/// only validate their immediate entity — they don't check the broader
/// context (parent lifecycle, project membership).
/// </remarks>
[Collection("Api0a Integration Tests")]
public class Api0aConsistencyBoundaryTests : ConsistencyBoundaryTestsBase<Api0aFixture>, IClassFixture<Api0aFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0a fixture.
    /// </summary>
    /// <param name="fixture">The Api0a fixture providing HTTP client and DB reset.</param>
    public Api0aConsistencyBoundaryTests(Api0aFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
