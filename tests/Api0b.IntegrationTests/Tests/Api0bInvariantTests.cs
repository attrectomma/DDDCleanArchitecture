using Api0b.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0b.IntegrationTests;

/// <summary>
/// Invariant enforcement integration tests for Api0b (Transaction Script + Concurrency Safety).
/// Inherits all shared invariant tests from <see cref="InvariantTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS (single-threaded).
/// Same as Api0a — the concurrency safety changes do not affect sequential
/// invariant enforcement.
/// </remarks>
[Collection("Api0b Integration Tests")]
public class Api0bInvariantTests : InvariantTestsBase<Api0bFixture>, IClassFixture<Api0bFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0b fixture.
    /// </summary>
    /// <param name="fixture">The Api0b fixture providing HTTP client and DB reset.</param>
    public Api0bInvariantTests(Api0bFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
