using Api4.Domain.ProjectAggregate;
using Api4.Domain.RetroAggregate;
using Api4.Domain.UserAggregate;
using Api4.Domain.VoteAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api4.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the API 4 (Split Aggregates) retro board database.
/// </summary>
/// <remarks>
/// DESIGN: Unlike API 3 which had 3 aggregate root DbSets, API 4 has 4.
/// The new <see cref="Votes"/> DbSet reflects that Vote is now its own
/// aggregate root. Child entities (Column, Note, ProjectMember) are still
/// accessed through navigation properties on their respective roots.
/// </remarks>
public class RetroBoardDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="RetroBoardDbContext"/>.
    /// </summary>
    /// <param name="options">The DbContext configuration options.</param>
    public RetroBoardDbContext(DbContextOptions<RetroBoardDbContext> options)
        : base(options)
    {
    }

    /// <summary>Gets the set of users (aggregate root).</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the set of projects (aggregate root).</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Gets the set of retro boards (aggregate root).</summary>
    public DbSet<RetroBoard> RetroBoards => Set<RetroBoard>();

    /// <summary>
    /// Gets the set of votes (aggregate root — NEW in API 4).
    /// </summary>
    /// <remarks>
    /// DESIGN: In API 3, Vote was a child entity within the RetroBoard aggregate
    /// and did not have its own DbSet. Now that Vote is an aggregate root,
    /// it needs a top-level DbSet for direct queries and persistence.
    /// </remarks>
    public DbSet<Vote> Votes => Set<Vote>();

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
