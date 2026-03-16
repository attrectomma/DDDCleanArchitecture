using Api0a.WebApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api0a.WebApi.Data;

/// <summary>
/// EF Core DbContext for the Api0a (Transaction Script) retro board database.
/// </summary>
/// <remarks>
/// DESIGN: Unlike API 1–5 which use separate <c>IEntityTypeConfiguration</c>
/// classes, all entity configuration lives inline in <see cref="OnModelCreating"/>.
/// For 7 entities this is perfectly readable and avoids the overhead of 7
/// configuration classes in 7 files. This is a deliberate simplicity choice —
/// at this scale, separate configuration classes are organizational overhead
/// with no functional benefit.
///
/// The unique indexes ARE present (they're part of the schema), but Api0a
/// does NOT catch the resulting <c>DbUpdateException</c> in middleware.
/// This means concurrent duplicates that bypass the application-level check
/// will bubble up as unhandled 500 errors. Api0b fixes this.
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
    /// Configures all entity mappings inline — no separate configuration classes.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ─────────────────────────────────────────────────
        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(300);
            builder.HasQueryFilter(u => u.DeletedAt == null);
        });

        // ── Project ──────────────────────────────────────────────
        modelBuilder.Entity<Project>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(300);

            builder.HasMany(p => p.RetroBoards)
                .WithOne(r => r.Project)
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.Members)
                .WithOne(m => m.Project)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(p => p.DeletedAt == null);
        });

        // ── ProjectMember ────────────────────────────────────────
        modelBuilder.Entity<ProjectMember>(builder =>
        {
            builder.HasKey(pm => pm.Id);

            builder.HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            builder.HasQueryFilter(pm => pm.DeletedAt == null);
        });

        // ── RetroBoard ───────────────────────────────────────────
        modelBuilder.Entity<RetroBoard>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Name).IsRequired().HasMaxLength(300);

            builder.HasOne(r => r.Project)
                .WithMany(p => p.RetroBoards)
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Columns)
                .WithOne(c => c.RetroBoard)
                .HasForeignKey(c => c.RetroBoardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(r => r.DeletedAt == null);
        });

        // ── Column ───────────────────────────────────────────────
        modelBuilder.Entity<Column>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).IsRequired().HasMaxLength(200);

            builder.HasOne(c => c.RetroBoard)
                .WithMany(r => r.Columns)
                .HasForeignKey(c => c.RetroBoardId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(c => c.Notes)
                .WithOne(n => n.Column)
                .HasForeignKey(n => n.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index: column names must be unique within a retro board
            builder.HasIndex(c => new { c.RetroBoardId, c.Name })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            builder.HasQueryFilter(c => c.DeletedAt == null);
        });

        // ── Note ─────────────────────────────────────────────────
        modelBuilder.Entity<Note>(builder =>
        {
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Text).IsRequired().HasMaxLength(2000);

            builder.HasOne(n => n.Column)
                .WithMany(c => c.Notes)
                .HasForeignKey(n => n.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(n => n.Votes)
                .WithOne(v => v.Note)
                .HasForeignKey(v => v.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index: note text must be unique within a column
            builder.HasIndex(n => new { n.ColumnId, n.Text })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            builder.HasQueryFilter(n => n.DeletedAt == null);
        });

        // ── Vote ─────────────────────────────────────────────────
        modelBuilder.Entity<Vote>(builder =>
        {
            builder.HasKey(v => v.Id);

            builder.HasOne(v => v.Note)
                .WithMany(n => n.Votes)
                .HasForeignKey(v => v.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.User)
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique index: one vote per user per note
            builder.HasIndex(v => new { v.NoteId, v.UserId })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            builder.HasQueryFilter(v => v.DeletedAt == null);
        });
    }
}
