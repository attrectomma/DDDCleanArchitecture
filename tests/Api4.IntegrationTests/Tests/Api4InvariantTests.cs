using Api4.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api4.IntegrationTests;

/// <summary>
/// Invariant enforcement tests for API 4 (Split Aggregates).
/// Inherits all shared invariant tests from <see cref="InvariantTestsBase{TFixture}"/>.
/// </summary>
public class Api4InvariantTests : InvariantTestsBase<Api4Fixture>, IClassFixture<Api4Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 4 fixture.
    /// </summary>
    /// <param name="fixture">The API 4 fixture providing HTTP client and DB reset.</param>
    public Api4InvariantTests(Api4Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
