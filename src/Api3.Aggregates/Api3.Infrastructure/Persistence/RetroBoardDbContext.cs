using Api3.Domain.ProjectAggregate;
using Api3.Domain.RetroAggregate;
using Api3.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;

namespace Api3.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the API 3 (Aggregate Design) retro board database.
/// </summary>
/// <remarks>
/// DESIGN: Unlike API 1/2, this DbContext only exposes DbSets for aggregate
/// roots. Child entities (Column, Note, Vote, ProjectMember) are accessed
/// through navigation properties on their respective aggregate roots, not
/// through standalone DbSets. EF Core still tracks and persists them.
///
/// We keep DbSets for aggregate roots only to emphasise the aggregate
/// boundaries. Queries always start from the root.
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
    /// Applies entity configurations from the current assembly.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RetroBoardDbContext).Assembly);
    }
}
