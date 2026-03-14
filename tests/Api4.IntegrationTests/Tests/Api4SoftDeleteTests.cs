using Api4.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api4.IntegrationTests;

/// <summary>
/// Soft delete tests for API 4 (Split Aggregates).
/// Inherits all shared soft delete tests from <see cref="SoftDeleteTestsBase{TFixture}"/>.
/// </summary>
public class Api4SoftDeleteTests : SoftDeleteTestsBase<Api4Fixture>, IClassFixture<Api4Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 4 fixture.
    /// </summary>
    /// <param name="fixture">The API 4 fixture providing HTTP client and DB reset.</param>
    public Api4SoftDeleteTests(Api4Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
