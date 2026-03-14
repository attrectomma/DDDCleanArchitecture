using Api5.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api5.IntegrationTests;

/// <summary>
/// Invariant enforcement tests for API 5 (Behavioral / CQRS / MediatR).
/// Inherits all shared invariant tests from <see cref="InvariantTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: API 5 enforces the same business invariants as API 3/4
/// (unique column names, unique note text, one vote per user per note,
/// no duplicate project members). The enforcement mechanism is different:
///   - Invariant checks live inside command handlers instead of service methods.
///   - FluentValidation validators run in the MediatR pipeline (via
///     <c>ValidationBehavior</c>) instead of ASP.NET model binding.
///   - DB unique constraints remain as safety nets.
///
/// From the HTTP contract perspective, all invariant violations produce
/// the same error responses (409 Conflict or 422 Unprocessable Entity).
/// </remarks>
public class Api5InvariantTests : InvariantTestsBase<Api5Fixture>, IClassFixture<Api5Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 5 fixture.
    /// </summary>
    /// <param name="fixture">The API 5 fixture providing HTTP client and DB reset.</param>
    public Api5InvariantTests(Api5Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
