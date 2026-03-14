using Api3.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetroBoard.IntegrationTests.Shared;
using RetroBoard.IntegrationTests.Shared.Fixtures;

namespace Api3.IntegrationTests.Fixtures;

/// <summary>
/// API 3-specific fixture that configures WebApplicationFactory
/// to use a Testcontainer Postgres database and applies EF Core migrations.
/// </summary>
/// <remarks>
/// DESIGN: Same pattern as Api1Fixture/Api2Fixture — this fixture manages
/// its own <see cref="PostgresFixture"/> internally. The only difference is
/// the DbContext type (<see cref="RetroBoardDbContext"/> from Api3.Infrastructure)
/// and the interceptor type.
/// </remarks>
public class Api3Fixture : ApiFixture<Program>
{
    private readonly PostgresFixture _ownedPostgresFixture;

    /// <summary>
    /// Initializes a new instance of <see cref="Api3Fixture"/>.
    /// Creates and manages its own <see cref="PostgresFixture"/>.
    /// </summary>
    public Api3Fixture() : this(new PostgresFixture())
    {
    }

    private Api3Fixture(PostgresFixture postgresFixture) : base(postgresFixture)
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
                sp.GetRequiredService<Api3.Infrastructure.Persistence.Interceptors.AuditInterceptor>());
        });
    }

    /// <inheritdoc />
    protected override async Task ApplyMigrationsAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RetroBoardDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
