using Xunit;

namespace RetroBoard.IntegrationTests.Shared.Fixtures;

/// <summary>
/// xUnit collection definition that groups all integration tests together
/// so they share a single <see cref="PostgresFixture"/> instance.
/// </summary>
/// <remarks>
/// DESIGN: Any test class decorated with
/// <c>[Collection(TestCollectionNames.IntegrationTests)]</c>
/// will receive the same <see cref="PostgresFixture"/> via constructor injection.
/// This avoids spinning up a separate Postgres container per test class.
/// </remarks>
[CollectionDefinition(TestCollectionNames.IntegrationTests)]
public class IntegrationTestCollection : ICollectionFixture<PostgresFixture>
{
    // This class has no code — it's a marker for xUnit's collection fixture wiring.
}
