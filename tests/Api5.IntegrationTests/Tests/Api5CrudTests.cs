using Api5.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api5.IntegrationTests;

/// <summary>
/// CRUD integration tests for API 5 (Behavioral / CQRS / MediatR).
/// Inherits all shared CRUD tests from <see cref="CrudTestsBase{TFixture}"/>
/// and provides the API 5-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: API 5 exposes the same REST contract as APIs 1–4, so every shared
/// CRUD test runs unchanged. The difference is purely internal: controllers
/// dispatch commands and queries via <c>IMediator</c> instead of calling
/// service interfaces directly. From the integration test perspective, the
/// behaviour is identical.
/// </remarks>
public class Api5CrudTests : CrudTestsBase<Api5Fixture>, IClassFixture<Api5Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 5 fixture.
    /// </summary>
    /// <param name="fixture">The API 5 fixture providing HTTP client and DB reset.</param>
    public Api5CrudTests(Api5Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
