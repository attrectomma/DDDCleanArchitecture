using Api2.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api2.IntegrationTests;

/// <summary>
/// Concurrency integration tests for API 2 (Rich Domain).
/// Inherits all shared concurrency tests from <see cref="ConcurrencyTestsBase{TFixture}"/>
/// and provides the API 2-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ⚠️ THESE TESTS ARE EXPECTED TO FAIL IN API 2. ⚠️
///
/// API 2 demonstrates rich domain entities with invariant enforcement, but
/// still lacks aggregate boundaries and optimistic concurrency tokens.
/// The same race conditions from API 1 apply:
///
/// Expected failures:
///   - Race conditions allow duplicate columns with the same name
///   - Race conditions allow duplicate votes on the same note
///   - No aggregate-level locking means "check-then-act" logic can be bypassed
///
/// The rich domain model makes the code more expressive, but without
/// aggregate boundaries (API 3+), consistency under concurrency is still broken.
///
/// Do NOT "fix" these failures by adding retry logic or locking.
/// The architectural solution is aggregate roots with concurrency tokens (API 3+).
/// </remarks>
[Collection("Api2 Integration Tests")]
public class Api2ConcurrencyTests : ConcurrencyTestsBase<Api2Fixture>, IClassFixture<Api2Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 2 fixture.
    /// </summary>
    /// <param name="fixture">The API 2 fixture providing HTTP client and DB reset.</param>
    public Api2ConcurrencyTests(Api2Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
