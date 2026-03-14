using Api4.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api4.IntegrationTests;

/// <summary>
/// CRUD integration tests for API 4 (Split Aggregates).
/// Inherits all shared CRUD tests from <see cref="CrudTestsBase{TFixture}"/>
/// and provides the API 4-specific fixture wiring.
/// </summary>
public class Api4CrudTests : CrudTestsBase<Api4Fixture>, IClassFixture<Api4Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 4 fixture.
    /// </summary>
    /// <param name="fixture">The API 4 fixture providing HTTP client and DB reset.</param>
    public Api4CrudTests(Api4Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
