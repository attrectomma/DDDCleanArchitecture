using Api3.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api3.IntegrationTests;

/// <summary>
/// Invariant enforcement tests for API 3 (Aggregate Design).
/// Inherits all shared invariant tests from <see cref="InvariantTestsBase{TFixture}"/>.
/// </summary>
public class Api3InvariantTests : InvariantTestsBase<Api3Fixture>, IClassFixture<Api3Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 3 fixture.
    /// </summary>
    /// <param name="fixture">The API 3 fixture providing HTTP client and DB reset.</param>
    public Api3InvariantTests(Api3Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
