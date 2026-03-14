using Api2.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api2.IntegrationTests;

/// <summary>
/// Soft delete tests for API 2 (Rich Domain).
/// Inherits all shared soft delete tests from <see cref="SoftDeleteTestsBase{TFixture}"/>.
/// </summary>
public class Api2SoftDeleteTests : SoftDeleteTestsBase<Api2Fixture>, IClassFixture<Api2Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 2 fixture.
    /// </summary>
    /// <param name="fixture">The API 2 fixture providing HTTP client and DB reset.</param>
    public Api2SoftDeleteTests(Api2Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
