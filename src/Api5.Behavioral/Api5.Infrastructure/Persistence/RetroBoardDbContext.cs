using Api5.Application.Common.Interfaces;
using Api5.Application.Common.Options;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.RetroAggregate;
using Api5.Domain.UserAggregate;
using Api5.Domain.VoteAggregate;
using Api5.Domain.VoteAggregate.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api5.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the API 5 (Behavioral / CQRS) retro board database.
/// Also implements <see cref="IReadOnlyDbContext"/> for CQRS query handlers.
/// </summary>
/// <remarks>
/// DESIGN: Same four aggregate root DbSets as API 4. The key addition is
/// that this DbContext implements <see cref="IReadOnlyDbContext"/>, which
/// exposes <see cref="IQueryable{T}"/> properties for every entity type.
/// CQRS query handlers depend on <see cref="IReadOnlyDbContext"/> rather
/// than this concrete class, keeping the Application layer free of
/// infrastructure concerns.
///
/// The DbContext serves both roles:
///   - Write side: Repositories use it via constructor injection for tracked
///     queries, adds, and deletes.
///   - Read side: Query handlers use it via <see cref="IReadOnlyDbContext"/>
///     for no-tracking projections.
///
/// In a production system with truly separate read/write stores, this
/// single DbContext would be replaced by two: one for writes (tracked)
/// and one for reads (no-tracking, possibly pointing at a read replica).
///
/// DESIGN: The DbContext receives <see cref="VotingOptions"/> via the Options
/// pattern to pass configuration to entity type configurations (specifically
/// <see cref="Configurations.VoteConfiguration"/>). This enables the Vote
/// entity's unique index to be conditioned on the configured default voting
/// strategy — when the default is <see cref="VotingStrategyType.Default"/>,
/// a unique index on <c>(NoteId, UserId)</c> is applied as a safety net
/// against concurrent duplicate votes.
/// </remarks>
public class RetroBoardDbContext : DbContext, IReadOnlyDbContext
{
    private readonly VotingOptions _votingOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardDbContext"/>.
    /// </summary>
    /// <param name="options">The DbContext configuration options.</param>
    /// <param name="votingOptions">
    /// The voting configuration options, used to determine whether to apply
    /// a unique index on Vote <c>(NoteId, UserId)</c>. Injected via the Options pattern.
    /// </param>
    public RetroBoardDbContext(
        DbContextOptions<RetroBoardDbContext> options,
        IOptions<VotingOptions> votingOptions)
        : base(options)
    {
        _votingOptions = votingOptions.Value;
    }

    // ── Aggregate root DbSets ───────────────────────────────────

    /// <summary>Gets the set of users (aggregate root).</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the set of projects (aggregate root).</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Gets the set of retro boards (aggregate root).</summary>
    public DbSet<RetroBoard> RetroBoards => Set<RetroBoard>();

    /// <summary>Gets the set of votes (aggregate root).</summary>
    public DbSet<Vote> Votes => Set<Vote>();

    // ── IReadOnlyDbContext implementation ────────────────────────
    // DESIGN (CQRS): These IQueryable properties expose read-only access
    // to entity sets for query handlers. Child entities (Column, Note,
    // ProjectMember) that are not DbSets are accessed via Set<T>().

    /// <inheritdoc />
    IQueryable<User> IReadOnlyDbContext.Users => Users;

    /// <inheritdoc />
    IQueryable<Project> IReadOnlyDbContext.Projects => Projects;

    /// <inheritdoc />
    IQueryable<ProjectMember> IReadOnlyDbContext.ProjectMembers => Set<ProjectMember>();

    /// <inheritdoc />
    IQueryable<RetroBoard> IReadOnlyDbContext.RetroBoards => RetroBoards;

    /// <inheritdoc />
    IQueryable<Column> IReadOnlyDbContext.Columns => Set<Column>();

    /// <inheritdoc />
    IQueryable<Note> IReadOnlyDbContext.Notes => Set<Note>();

    /// <inheritdoc />
    IQueryable<Vote> IReadOnlyDbContext.Votes => Votes;

    // ── Model configuration ─────────────────────────────────────

    /// <summary>
    /// Gets the voting options for use by entity type configurations.
    /// </summary>
    internal VotingOptions VotingOptions => _votingOptions;

    /// <summary>
    /// Applies entity configurations from the current assembly and conditionally
    /// modifies the Vote entity's index based on the configured voting strategy.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <remarks>
    /// DESIGN: After applying all <c>IEntityTypeConfiguration</c> classes, this
    /// method checks the configured <see cref="VotingOptions.DefaultVotingStrategy"/>.
    /// When the default strategy is <see cref="VotingStrategyType.Default"/>,
    /// the <c>(NoteId, UserId)</c> index on Vote is made UNIQUE so the database
    /// acts as a safety net against concurrent duplicate votes. When the default
    /// is <see cref="VotingStrategyType.Budget"/>, the index remains non-unique
    /// because budget voting allows multiple votes per user per note.
    ///
    /// This is a key educational point: the Options pattern not only configures
    /// application behaviour but can also influence the database schema. In a
    /// production system using migrations, you would generate separate migration
    /// branches for each configuration. In this educational context (tests use
    /// <c>EnsureCreatedAsync</c>), the schema adapts automatically.
    /// </remarks>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RetroBoardDbContext).Assembly);

        // DESIGN: Conditionally make the Vote (NoteId, UserId) index unique
        // when the default strategy is "Default". This restores the DB safety
        // net that prevents concurrent duplicate votes under the Default strategy.
        // When Budget is the default, the index stays non-unique so dot voting works.
        if (_votingOptions.DefaultVotingStrategy == VotingStrategyType.Default)
        {
            modelBuilder.Entity<Vote>(builder =>
            {
                builder.HasIndex(v => new { v.NoteId, v.UserId })
                    .HasDatabaseName("IX_Vote_NoteId_UserId")
                    .IsUnique()
                    .HasFilter("\"DeletedAt\" IS NULL");
            });
        }
    }
}
