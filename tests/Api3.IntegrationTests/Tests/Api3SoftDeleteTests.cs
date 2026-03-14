using Api3.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api3.IntegrationTests;

/// <summary>
/// Soft delete tests for API 3 (Aggregate Design).
/// Inherits all shared soft delete tests from <see cref="SoftDeleteTestsBase{TFixture}"/>.
/// </summary>
public class Api3SoftDeleteTests : SoftDeleteTestsBase<Api3Fixture>, IClassFixture<Api3Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 3 fixture.
    /// </summary>
    /// <param name="fixture">The API 3 fixture providing HTTP client and DB reset.</param>
    public Api3SoftDeleteTests(Api3Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
