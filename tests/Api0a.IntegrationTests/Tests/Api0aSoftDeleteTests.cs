using Api0a.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0a.IntegrationTests;

/// <summary>
/// Soft-delete integration tests for Api0a (Transaction Script).
/// Inherits all shared soft-delete tests from <see cref="SoftDeleteTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS.
/// Soft delete is handled by the <c>AuditInterceptor</c> — the same
/// cross-cutting mechanism used in API 1–5. The global query filter
/// (<c>DeletedAt == null</c>) ensures soft-deleted entities are invisible.
/// </remarks>
[Collection("Api0a Integration Tests")]
public class Api0aSoftDeleteTests : SoftDeleteTestsBase<Api0aFixture>, IClassFixture<Api0aFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0a fixture.
    /// </summary>
    /// <param name="fixture">The Api0a fixture providing HTTP client and DB reset.</param>
    public Api0aSoftDeleteTests(Api0aFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
