using Api3.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api3.IntegrationTests;

/// <summary>
/// Concurrency integration tests for API 3 (Aggregate Design).
/// Inherits all shared concurrency tests from <see cref="ConcurrencyTestsBase{TFixture}"/>
/// and provides the API 3-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS IN API 3. ✅
///
/// API 3 introduces proper aggregate boundaries with optimistic concurrency
/// via PostgreSQL's xmin system column. When two concurrent requests try to
/// modify the same aggregate:
///
///   1. Both load the aggregate with the same xmin value.
///   2. The first request saves successfully, bumping the xmin.
///   3. The second request's SaveChanges detects the xmin mismatch and throws
///      <c>DbUpdateConcurrencyException</c>, which is caught by
///      <c>ConcurrencyConflictMiddleware</c> and returned as 409 Conflict.
///
/// This is the architectural solution to the race conditions that plagued
/// API 1 and API 2. The trade-off is that ANY two writes to the same retro
/// board will conflict — even if they touch different columns/notes.
/// API 4 addresses this by extracting Vote into its own aggregate.
/// </remarks>
[Collection("Api3 Integration Tests")]
public class Api3ConcurrencyTests : ConcurrencyTestsBase<Api3Fixture>, IClassFixture<Api3Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 3 fixture.
    /// </summary>
    /// <param name="fixture">The API 3 fixture providing HTTP client and DB reset.</param>
    public Api3ConcurrencyTests(Api3Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
