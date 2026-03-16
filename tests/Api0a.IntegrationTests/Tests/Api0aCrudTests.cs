using Api0a.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0a.IntegrationTests;

/// <summary>
/// CRUD integration tests for Api0a (Transaction Script).
/// Inherits all shared CRUD tests from <see cref="CrudTestsBase{TFixture}"/>
/// and provides the Api0a-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS.
/// The Transaction Script API exposes the same REST contract as API 1–5.
/// CRUD operations work correctly — the simplicity of the approach does
/// not compromise basic functionality.
/// </remarks>
[Collection("Api0a Integration Tests")]
public class Api0aCrudTests : CrudTestsBase<Api0aFixture>, IClassFixture<Api0aFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0a fixture.
    /// </summary>
    /// <param name="fixture">The Api0a fixture providing HTTP client and DB reset.</param>
    public Api0aCrudTests(Api0aFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
