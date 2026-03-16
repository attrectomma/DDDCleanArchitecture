using Api0a.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0a.IntegrationTests;

/// <summary>
/// Concurrency integration tests for Api0a (Transaction Script).
/// Inherits all shared concurrency tests from <see cref="ConcurrencyTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: ⚠️ THESE TESTS ARE EXPECTED TO FAIL IN Api0a. ⚠️
///
/// Api0a uses the same non-atomic check-then-act pattern as API 1/2.
/// Concurrent requests can both pass the uniqueness check and create
/// duplicates. The DB unique indexes exist but the resulting
/// <c>DbUpdateException</c> is not caught by the middleware, so the
/// second request gets a 500 instead of a 409.
///
/// Api0b fixes this with ~35 lines of changes:
///   - xmin concurrency tokens on key entities
///   - Middleware catch blocks for <c>DbUpdateConcurrencyException</c>
///     and <c>DbUpdateException</c> (PostgreSQL error 23505)
///
/// The same concurrency tests PASS in Api0b — without changing any
/// endpoint handler code. That's the teaching point.
/// </remarks>
[Collection("Api0a Integration Tests")]
public class Api0aConcurrencyTests : ConcurrencyTestsBase<Api0aFixture>, IClassFixture<Api0aFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0a fixture.
    /// </summary>
    /// <param name="fixture">The Api0a fixture providing HTTP client and DB reset.</param>
    public Api0aConcurrencyTests(Api0aFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
