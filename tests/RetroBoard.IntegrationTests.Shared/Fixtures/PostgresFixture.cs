using Testcontainers.PostgreSql;
using Xunit;

namespace RetroBoard.IntegrationTests.Shared.Fixtures;

/// <summary>
/// xUnit collection fixture that manages the lifecycle of a PostgreSQL
/// Testcontainer. Shared across all test classes in a collection so the
/// container is started once and reused.
/// </summary>
/// <remarks>
/// DESIGN: Using Testcontainers ensures every test run gets a clean,
/// isolated Postgres instance — no dependency on a pre-existing database.
/// The container is started in <see cref="InitializeAsync"/> and disposed
/// after all tests in the collection have completed.
/// </remarks>
public class PostgresFixture : IAsyncLifetime
{
    /// <summary>
    /// The Testcontainers Postgres container instance.
    /// </summary>
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("retroboard_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>
    /// Gets the connection string to the running Postgres container.
    /// Available after <see cref="InitializeAsync"/> completes.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Starts the Postgres container. Called once by xUnit before
    /// any test in the collection runs.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    /// <summary>
    /// Stops and removes the Postgres container. Called once by xUnit
    /// after all tests in the collection have completed.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
