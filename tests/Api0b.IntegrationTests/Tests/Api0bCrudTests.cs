using Api0b.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0b.IntegrationTests;

/// <summary>
/// CRUD integration tests for Api0b (Transaction Script + Concurrency Safety).
/// Inherits all shared CRUD tests from <see cref="CrudTestsBase{TFixture}"/>
/// and provides the Api0b-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS.
/// Same as Api0a — the concurrency safety changes do not affect basic CRUD.
/// </remarks>
[Collection("Api0b Integration Tests")]
public class Api0bCrudTests : CrudTestsBase<Api0bFixture>, IClassFixture<Api0bFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0b fixture.
    /// </summary>
    /// <param name="fixture">The Api0b fixture providing HTTP client and DB reset.</param>
    public Api0bCrudTests(Api0bFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
