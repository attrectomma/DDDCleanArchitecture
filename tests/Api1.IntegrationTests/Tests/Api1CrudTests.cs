using Api1.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api1.IntegrationTests;

/// <summary>
/// CRUD integration tests for API 1 (Anemic CRUD).
/// Inherits all shared CRUD tests from <see cref="CrudTestsBase{TFixture}"/>
/// and provides the API 1-specific fixture wiring.
/// </summary>
public class Api1CrudTests : CrudTestsBase<Api1Fixture>, IClassFixture<Api1Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 1 fixture.
    /// </summary>
    /// <param name="fixture">The API 1 fixture providing HTTP client and DB reset.</param>
    public Api1CrudTests(Api1Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
