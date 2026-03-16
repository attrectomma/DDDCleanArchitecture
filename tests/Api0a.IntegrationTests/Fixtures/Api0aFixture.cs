using Api0a.WebApi.Data;
using Api0a.WebApi.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetroBoard.IntegrationTests.Shared;
using RetroBoard.IntegrationTests.Shared.Fixtures;

namespace Api0a.IntegrationTests.Fixtures;

/// <summary>
/// Api0a-specific fixture that configures WebApplicationFactory
/// to use a Testcontainer Postgres database and applies EF Core migrations.
/// </summary>
/// <remarks>
/// DESIGN: Same fixture pattern as Api1–5 test projects. The fixture
/// manages its own <see cref="PostgresFixture"/> internally. The only
/// API-specific code is the DbContext replacement in <see cref="ConfigureServices"/>.
/// This demonstrates that the shared test infrastructure works with ANY
/// API implementation that exposes the same REST contract — whether it's
/// a 4-project Clean Architecture app or a single-project Transaction Script.
/// </remarks>
public class Api0aFixture : ApiFixture<Program>
{
    private readonly PostgresFixture _ownedPostgresFixture;

    /// <summary>
    /// Initializes a new instance of <see cref="Api0aFixture"/>.
    /// Creates and manages its own <see cref="PostgresFixture"/>.
    /// </summary>
    public Api0aFixture() : this(new PostgresFixture())
    {
    }

    private Api0aFixture(PostgresFixture postgresFixture) : base(postgresFixture)
    {
        _ownedPostgresFixture = postgresFixture;
    }

    /// <summary>
    /// Starts the Postgres container, then initialises the API fixture.
    /// </summary>
    public override async Task InitializeAsync()
    {
        await _ownedPostgresFixture.InitializeAsync();
        await base.InitializeAsync();
    }

    /// <summary>
    /// Disposes the API fixture and then stops the Postgres container.
    /// </summary>
    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _ownedPostgresFixture.DisposeAsync();
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Remove the existing DbContext registration and replace with test connection string
        ServiceDescriptor? descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<RetroBoardDbContext>));

        if (descriptor is not null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<RetroBoardDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString);
            options.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>());
        });
    }

    /// <inheritdoc />
    protected override async Task ApplyMigrationsAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        RetroBoardDbContext dbContext = scope.ServiceProvider.GetRequiredService<RetroBoardDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
