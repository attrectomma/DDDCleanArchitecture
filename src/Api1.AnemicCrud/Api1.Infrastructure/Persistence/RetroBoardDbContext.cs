using Api1.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api1.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the API 1 (Anemic CRUD) retro board database.
/// </summary>
/// <remarks>
/// DESIGN: In API 1 the DbContext exposes a <see cref="DbSet{TEntity}"/>
/// for every entity in the domain. Each entity has its own repository that
/// wraps the corresponding DbSet. This 1-to-1 mapping is intentional —
/// it mirrors the "table → entity → repository" approach common in anemic
/// CRUD applications. API 3+ reduce the number of DbSets exposed by
/// loading child entities through aggregate roots instead.
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

    /// <summary>Gets the set of users.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Gets the set of projects.</summary>
    public DbSet<Project> Projects => Set<Project>();

    /// <summary>Gets the set of project memberships.</summary>
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

    /// <summary>Gets the set of retro boards.</summary>
    public DbSet<RetroBoard> RetroBoards => Set<RetroBoard>();

    /// <summary>Gets the set of columns.</summary>
    public DbSet<Column> Columns => Set<Column>();

    /// <summary>Gets the set of notes.</summary>
    public DbSet<Note> Notes => Set<Note>();

    /// <summary>Gets the set of votes.</summary>
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
