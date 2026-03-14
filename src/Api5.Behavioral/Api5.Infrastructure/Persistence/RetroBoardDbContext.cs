using Api5.Application.Common.Interfaces;
using Api5.Domain.ProjectAggregate;
using Api5.Domain.RetroAggregate;
using Api5.Domain.UserAggregate;
using Api5.Domain.VoteAggregate;
using Microsoft.EntityFrameworkCore;

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
/// </remarks>
public class RetroBoardDbContext : DbContext, IReadOnlyDbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardDbContext"/>.
    /// </summary>
    /// <param name="options">The DbContext configuration options.</param>
    public RetroBoardDbContext(DbContextOptions<RetroBoardDbContext> options)
        : base(options)
    {
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
    /// Applies entity configurations from the current assembly.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RetroBoardDbContext).Assembly);
    }
}
