using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api2.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the API 2 (Rich Domain) retro board database.
/// </summary>
/// <remarks>
/// DESIGN: Identical structure to API 1 — a DbSet per entity. The key
/// difference is that entity configurations now use
/// <c>PropertyAccessMode.Field</c> to map private backing fields for
/// collections (e.g., <c>_notes</c>, <c>_votes</c>, <c>_members</c>).
/// This allows EF Core to track changes when domain methods add/remove
/// items from these collections.
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
