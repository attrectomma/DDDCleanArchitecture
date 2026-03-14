using Api5.Infrastructure.Persistence;
using Api5.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RetroBoard.IntegrationTests.Shared;
using RetroBoard.IntegrationTests.Shared.Fixtures;

namespace Api5.IntegrationTests.Fixtures;

/// <summary>
/// API 5-specific fixture that configures WebApplicationFactory
/// to use a Testcontainer Postgres database and applies EF Core migrations.
/// </summary>
/// <remarks>
/// DESIGN: Same pattern as Api3Fixture/Api4Fixture — this fixture manages its
/// own <see cref="PostgresFixture"/> internally. The key difference from API 4
/// is that the DbContext registration must include both the
/// <see cref="AuditInterceptor"/> (Singleton) and the
/// <see cref="DomainEventInterceptor"/> (Scoped). The
/// DomainEventInterceptor depends on MediatR's <c>IPublisher</c>, so it
/// must be resolved from the scoped service provider at DbContext creation time.
///
/// API 5 uses the same REST contract as APIs 1–4, so all shared test base
/// classes run unchanged.
/// </remarks>
public class Api5Fixture : ApiFixture<Program>
{
    private readonly PostgresFixture _ownedPostgresFixture;

    /// <summary>
    /// Initializes a new instance of <see cref="Api5Fixture"/>.
    /// Creates and manages its own <see cref="PostgresFixture"/>.
    /// </summary>
    public Api5Fixture() : this(new PostgresFixture())
    {
    }

    private Api5Fixture(PostgresFixture postgresFixture) : base(postgresFixture)
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

        // DESIGN: Must re-register DbContext with both interceptors.
        // The AuditInterceptor is Singleton and the DomainEventInterceptor
        // is Scoped (because it depends on MediatR's IPublisher).
        services.AddDbContext<RetroBoardDbContext>((sp, options) =>
        {
            options.UseNpgsql(ConnectionString);
            options.AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<DomainEventInterceptor>());
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
