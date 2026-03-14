using Api5.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api5.IntegrationTests;

/// <summary>
/// Concurrency integration tests for API 5 (Behavioral / CQRS / MediatR).
/// Inherits all shared concurrency tests from <see cref="ConcurrencyTestsBase{TFixture}"/>
/// and provides the API 5-specific fixture wiring.
/// </summary>
/// <remarks>
/// DESIGN: ✅ THESE TESTS ARE EXPECTED TO PASS IN API 5. ✅
///
/// API 5 maintains the same optimistic concurrency guarantees as API 3/4
/// via xmin tokens on aggregate roots. The concurrency protection is
/// unchanged — it lives in the Infrastructure layer (EF Core configurations)
/// which is identical to API 4.
///
/// The difference is that concurrency conflicts bubble up through MediatR
/// command handlers rather than service methods. The
/// <c>ConcurrencyConflictMiddleware</c> catches
/// <c>DbUpdateConcurrencyException</c> and maps it to 409 Conflict,
/// just as in API 3/4.
///
/// Key concurrency behaviors:
///   - Column/note operations on the same retro board: conflict (xmin on RetroBoard).
///   - Vote operations on different notes: NO conflict (Vote is its own aggregate).
///   - Duplicate vote by same user: rejected by DB unique constraint.
/// </remarks>
[Collection("Api5 Integration Tests")]
public class Api5ConcurrencyTests : ConcurrencyTestsBase<Api5Fixture>, IClassFixture<Api5Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 5 fixture.
    /// </summary>
    /// <param name="fixture">The API 5 fixture providing HTTP client and DB reset.</param>
    public Api5ConcurrencyTests(Api5Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
