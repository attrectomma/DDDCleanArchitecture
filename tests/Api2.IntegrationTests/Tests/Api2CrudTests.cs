using Api2.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api2.IntegrationTests;

/// <summary>
/// CRUD integration tests for API 2 (Rich Domain).
/// Inherits all shared CRUD tests from <see cref="CrudTestsBase{TFixture}"/>
/// and provides the API 2-specific fixture wiring.
/// </summary>
public class Api2CrudTests : CrudTestsBase<Api2Fixture>, IClassFixture<Api2Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 2 fixture.
    /// </summary>
    /// <param name="fixture">The API 2 fixture providing HTTP client and DB reset.</param>
    public Api2CrudTests(Api2Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
