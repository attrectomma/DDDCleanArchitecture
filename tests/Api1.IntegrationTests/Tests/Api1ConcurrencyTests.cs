using Api1.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api1.IntegrationTests;

/// <summary>
/// Concurrency integration tests for API 1 (Anemic CRUD).
/// Inherits all shared concurrency tests from <see cref="ConcurrencyTestsBase{TFixture}"/>
/// and provides the API 1-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ⚠️ THESE TESTS ARE EXPECTED TO FAIL IN API 1. ⚠️
/// 
/// API 1 demonstrates the simplest possible architecture (anemic domain, service-layer
/// business logic, no optimistic concurrency). These tests exist to show the shortcomings
/// of this approach when handling concurrent requests.
/// 
/// Expected failures:
///   - Race conditions allow duplicate columns with the same name
///   - Race conditions allow duplicate votes on the same note
///   - No aggregate-level locking means "check-then-act" logic can be bypassed
/// 
/// The same tests will PASS in API 3+ where:
///   - Aggregates enforce invariants
///   - Optimistic concurrency (xmin tokens) prevents parallel modifications
///   - Database constraints act as safety nets, not primary enforcement
/// 
/// Do NOT "fix" these failures by adding retry logic or better service-layer checks.
/// The point is to demonstrate that architectural improvements (aggregates, concurrency
/// tokens) are needed, not band-aids.
/// </remarks>
[Collection("Api1 Integration Tests")]
public class Api1ConcurrencyTests : ConcurrencyTestsBase<Api1Fixture>, IClassFixture<Api1Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 1 fixture.
    /// </summary>
    /// <param name="fixture">The API 1 fixture providing HTTP client and DB reset.</param>
    public Api1ConcurrencyTests(Api1Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
