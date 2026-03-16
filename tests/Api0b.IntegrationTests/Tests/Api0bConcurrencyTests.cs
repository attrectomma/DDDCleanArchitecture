using Api0b.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api0b.IntegrationTests;

/// <summary>
/// Concurrency integration tests for Api0b (Transaction Script + Concurrency Safety).
/// Inherits all shared concurrency tests from <see cref="ConcurrencyTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS IN Api0b. ✅
///
/// This is the milestone. The same concurrency tests that FAIL in Api0a
/// PASS here — without changing any endpoint handler code. The fix is
/// entirely in:
///   - Entity configuration: xmin concurrency tokens on User, Project, RetroBoard
///   - Middleware: catch blocks for <c>DbUpdateConcurrencyException</c>
///     and <c>DbUpdateException</c> (PostgreSQL error 23505)
///
/// Compare with the DDD path where fixing these same tests required:
///   - API 1 → API 2: Rich domain entities, private setters, guard methods
///   - API 2 → API 3: Aggregate roots, aggregate-level repositories,
///     full graph loading, xmin on aggregate roots
///
/// Api0b achieves the same observable HTTP behavior with ~35 lines of
/// configuration and middleware changes. That's the teaching point.
/// </remarks>
[Collection("Api0b Integration Tests")]
public class Api0bConcurrencyTests : ConcurrencyTestsBase<Api0bFixture>, IClassFixture<Api0bFixture>
{
    /// <summary>
    /// Initializes the test class with the Api0b fixture.
    /// </summary>
    /// <param name="fixture">The Api0b fixture providing HTTP client and DB reset.</param>
    public Api0bConcurrencyTests(Api0bFixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
