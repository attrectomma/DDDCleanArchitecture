using Api5.IntegrationTests.Fixtures;
using RetroBoard.IntegrationTests.Shared.Tests;
using Xunit;

namespace Api5.IntegrationTests;

/// <summary>
/// Soft delete tests for API 5 (Behavioral / CQRS / MediatR).
/// Inherits all shared soft delete tests from <see cref="SoftDeleteTestsBase{TFixture}"/>.
/// </summary>
/// <remarks>
/// DESIGN: API 5 soft-deletes via the same <c>AuditInterceptor</c> as
/// APIs 1–4. The interceptor converts <c>EntityState.Deleted</c> into a
/// <c>EntityState.Modified</c> with <c>DeletedAt</c> set.
///
/// The key API 5 addition is domain events: when a note is removed, the
/// <c>NoteRemovedEvent</c> is dispatched by the <c>DomainEventInterceptor</c>
/// after save, and the <c>NoteRemovedEventHandler</c> cascades the
/// soft-delete to associated votes. This means "delete note → votes are
/// cleaned up" happens via event-driven side effects rather than inline
/// service logic (API 3/4 approach).
/// </remarks>
public class Api5SoftDeleteTests : SoftDeleteTestsBase<Api5Fixture>, IClassFixture<Api5Fixture>
{
    /// <summary>
    /// Initializes the test class with the API 5 fixture.
    /// </summary>
    /// <param name="fixture">The API 5 fixture providing HTTP client and DB reset.</param>
    public Api5SoftDeleteTests(Api5Fixture fixture)
        : base(fixture.Client, fixture.ResetDatabaseAsync)
    {
    }
}
