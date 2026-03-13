using Api1.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api1.IntegrationTests;

/// <summary>
/// Soft delete tests for API 1 (Anemic CRUD).
/// Inherits all shared soft delete tests from <see cref="SoftDeleteTestsBase{TFixture}"/>.
/// </summary>
public class Api1SoftDeleteTests : SoftDeleteTestsBase<Api1Fixture>, IClassFixture<Api1Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 1 fixture.
    /// </summary>
    /// <param name="fixture">The API 1 fixture providing HTTP client and DB reset.</param>
    public Api1SoftDeleteTests(Api1Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
