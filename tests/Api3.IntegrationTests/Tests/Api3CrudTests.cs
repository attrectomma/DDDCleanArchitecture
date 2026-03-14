using Api3.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api3.IntegrationTests;

/// <summary>
/// CRUD integration tests for API 3 (Aggregate Design).
/// Inherits all shared CRUD tests from <see cref="CrudTestsBase{TFixture}"/>
/// and provides the API 3-specific fixture wiring.
/// </summary>
public class Api3CrudTests : CrudTestsBase<Api3Fixture>, IClassFixture<Api3Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 3 fixture.
    /// </summary>
    /// <param name="fixture">The API 3 fixture providing HTTP client and DB reset.</param>
    public Api3CrudTests(Api3Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
