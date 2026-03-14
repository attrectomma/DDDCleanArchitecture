using Api2.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api2.IntegrationTests;

/// <summary>
/// Invariant enforcement tests for API 2 (Rich Domain).
/// Inherits all shared invariant tests from <see cref="InvariantTestsBase{TFixture}"/>.
/// </summary>
public class Api2InvariantTests : InvariantTestsBase<Api2Fixture>, IClassFixture<Api2Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 2 fixture.
    /// </summary>
    /// <param name="fixture">The API 2 fixture providing HTTP client and DB reset.</param>
    public Api2InvariantTests(Api2Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
