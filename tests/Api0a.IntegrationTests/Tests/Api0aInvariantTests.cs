using Api0a.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0a.IntegrationTests;

/// <summary>
/// Invariant enforcement integration tests for Api0a (Transaction Script).
/// Inherits all shared invariant tests from <see cref="InvariantTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS (single-threaded).
/// The Transaction Script endpoints enforce invariants via check-then-act
/// queries — which work correctly when requests arrive sequentially.
/// The concurrency tests (see <see cref="Api0aConcurrencyTests"/>) expose
/// the race conditions that make these checks unsafe under parallel access.
/// </remarks>
[Collection("Api0a Integration Tests")]
public class Api0aInvariantTests : InvariantTestsBase<Api0aFixture>, IClassFixture<Api0aFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0a fixture.
    /// </summary>
    /// <param name="fixture">The Api0a fixture providing HTTP client and DB reset.</param>
    public Api0aInvariantTests(Api0aFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
