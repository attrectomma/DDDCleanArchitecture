using Api5.Application.Common.Options;
using Api5.Domain.VoteAggregate.Strategies;
using Api5.Infrastructure.Persistence;
using Api5.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using RetroBoard.IntegrationTests.Shared;
using RetroBoard.IntegrationTests.Shared.Fixtures;

namespace Api5.IntegrationTests.Fixtures;

/// <summary>
/// API 5 fixture configured for the Budget voting strategy.
/// Creates a database schema WITHOUT a unique index on Vote (NoteId, UserId)
/// so that the <see cref="BudgetVotingStrategy"/> can allow multiple votes
/// per user per note ("dot voting").
/// </summary>
/// <remarks>
/// DESIGN: This fixture demonstrates an important aspect of the Options pattern:
/// the same application code can behave differently based on configuration.
/// The shared <see cref="Api5Fixture"/> configures the Default strategy (with
/// a unique vote constraint), while this fixture configures the Budget strategy
/// (without the constraint). Each fixture represents a different deployment
/// configuration.
///
/// In production, an operator would choose one strategy via <c>appsettings.json</c>.
/// In tests, we use separate fixtures to validate both configurations.
/// </remarks>
public class Api5BudgetFixture : ApiFixture<Program>
{
    private readonly PostgresFixture _ownedPostgresFixture;

    /// <summary>
    /// Initializes a new instance of <see cref="Api5BudgetFixture"/>.
    /// Creates and manages its own <see cref="PostgresFixture"/>.
    /// </summary>
    public Api5BudgetFixture() : this(new PostgresFixture())
    {
    }

    private Api5BudgetFixture(PostgresFixture postgresFixture) : base(postgresFixture)
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
    /// <remarks>
    /// DESIGN: Configures <see cref="VotingOptions"/> with
    /// <see cref="VotingStrategyType.Budget"/> as the default strategy.
    /// This means the DbContext will NOT apply a unique index on
    /// <c>Vote(NoteId, UserId)</c>, allowing the Budget strategy's dot voting
    /// behaviour (multiple votes per user per note).
    /// </remarks>
    protected override void ConfigureServices(IServiceCollection services)
    {
        // ── VotingOptions: Budget strategy without unique vote constraint ──
        services.Configure<VotingOptions>(opts =>
        {
            opts.DefaultVotingStrategy = VotingStrategyType.Budget;
            opts.MaxVotesPerColumn = 3;
        });

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
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<DomainEventInterceptor>());
            options.ReplaceService<IModelCacheKeyFactory, VotingStrategyModelCacheKeyFactory>();
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
