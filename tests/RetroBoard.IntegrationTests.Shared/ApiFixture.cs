using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using RetroBoard.IntegrationTests.Shared.Fixtures;
using Xunit;

namespace RetroBoard.IntegrationTests.Shared;

/// <summary>
/// Generic API fixture that wraps <see cref="WebApplicationFactory{TEntryPoint}"/>
/// for any of the five API projects. Handles:
/// <list type="bullet">
///   <item>Replacing the DB connection string to point at the Testcontainer.</item>
///   <item>Applying EF Core migrations on startup.</item>
///   <item>Resetting the database between tests via Respawn.</item>
/// </list>
/// </summary>
/// <typeparam name="TProgram">
/// The <c>Program</c> class (entry point) of the API under test.
/// Each API project must expose its Program class via
/// <c>&lt;InternalsVisibleTo Include="..." /&gt;</c> or make it public.
/// </typeparam>
/// <remarks>
/// DESIGN: This class is the core of the shared test infrastructure.
/// Each API-specific test project creates a thin subclass that provides
/// <typeparamref name="TProgram"/> and optionally overrides
/// <see cref="ConfigureServices"/> for API-specific DI tweaks.
/// </remarks>
public abstract class ApiFixture<TProgram> : IAsyncLifetime
    where TProgram : class
{
    private readonly PostgresFixture _postgresFixture;
    private WebApplicationFactory<TProgram>? _factory;
    private Respawner? _respawner;

    /// <summary>
    /// Initializes a new instance of the API fixture.
    /// </summary>
    /// <param name="postgresFixture">
    /// The shared Postgres container fixture provided by xUnit's collection wiring.
    /// </param>
    protected ApiFixture(PostgresFixture postgresFixture)
    {
        _postgresFixture = postgresFixture;
    }

    /// <summary>
    /// Gets an <see cref="HttpClient"/> configured to call the API under test.
    /// </summary>
    public HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Gets the connection string to the Postgres test database.
    /// </summary>
    public string ConnectionString => _postgresFixture.ConnectionString;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> of the running test server.
    /// Useful for resolving scoped services (e.g., DbContext) in test assertions.
    /// </summary>
    public IServiceProvider Services => _factory!.Services;

    /// <summary>
    /// Sets up the <see cref="WebApplicationFactory{TEntryPoint}"/>, applies
    /// migrations, creates the HTTP client, and initializes Respawn.
    /// Override in subclasses to add pre-initialization steps (e.g., starting
    /// a Postgres container) before calling <c>base.InitializeAsync()</c>.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<TProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    // Allow API-specific DI overrides
                    ConfigureServices(services);
                });
            });

        Client = _factory.CreateClient();

        // Apply EF Core migrations so the schema exists
        await ApplyMigrationsAsync();

        // Initialize Respawn for fast DB resets between tests
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });
    }

    /// <summary>
    /// Override in API-specific subclasses to customize DI registration
    /// (e.g., replace the connection string in the DbContext).
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Override in API-specific subclasses to apply EF Core migrations
    /// using the correct DbContext type.
    /// </summary>
    protected abstract Task ApplyMigrationsAsync();

    /// <summary>
    /// Resets the database to a clean state. Call this in the test constructor
    /// or in a base test class's setup method to ensure test isolation.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        if (_respawner is not null)
        {
            await _respawner.ResetAsync(connection);
        }
    }

    /// <summary>
    /// Disposes the <see cref="WebApplicationFactory{TEntryPoint}"/> and HTTP client.
    /// Override in subclasses to add post-disposal steps (e.g., stopping
    /// a Postgres container) after calling <c>base.DisposeAsync()</c>.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        Client.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }
}
