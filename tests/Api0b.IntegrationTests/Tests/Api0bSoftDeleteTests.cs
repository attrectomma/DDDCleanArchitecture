using Api0b.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0b.IntegrationTests;

/// <summary>
/// Soft-delete integration tests for Api0b (Transaction Script + Concurrency Safety).
/// Inherits all shared soft-delete tests from <see cref="SoftDeleteTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS.
/// Same as Api0a — soft delete is handled by the <c>AuditInterceptor</c>,
/// which is unchanged in Api0b.
/// </remarks>
[Collection("Api0b Integration Tests")]
public class Api0bSoftDeleteTests : SoftDeleteTestsBase<Api0bFixture>, IClassFixture<Api0bFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0b fixture.
    /// </summary>
    /// <param name="fixture">The Api0b fixture providing HTTP client and DB reset.</param>
    public Api0bSoftDeleteTests(Api0bFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
