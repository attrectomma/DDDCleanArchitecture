using Api3.Domain.RetroAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api3.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="RetroBoard"/> aggregate root
/// and its child entities (<see cref="Column"/>, <see cref="Note"/>, <see cref="Vote"/>).
/// </summary>
/// <remarks>
/// DESIGN: All entities within the RetroBoard aggregate are configured together
/// to make the aggregate boundary visible. The aggregate root uses
/// <see cref="NpgsqlEntityTypeBuilderExtensions.UseXminAsConcurrencyToken"/>
/// for optimistic concurrency — any write to ANY entity in the aggregate
/// (adding a column, renaming a note, casting a vote) goes through the root
/// and bumps its xmin. Concurrent writes to the same retro board will conflict.
///
/// This is the trade-off of a large aggregate: strong consistency at the cost
/// of reduced write throughput. API 4 addresses this by extracting Vote.
/// </remarks>
public class RetroBoardConfiguration : IEntityTypeConfiguration<RetroBoard>
{
    /// <summary>Configures the RetroBoard entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<RetroBoard> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(300);

        // DESIGN: xmin is a PostgreSQL system column that changes on every
        // row update. Using it as a concurrency token means that if two
        // requests load the same retro, the second SaveChanges will throw
        // DbUpdateConcurrencyException. This is how we enforce the
        // aggregate as a consistency boundary.
        builder.Property(r => r.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(r => r.Columns)
            .WithOne()
            .HasForeignKey(c => c.RetroBoardId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF Core to use the backing field for the Columns navigation
        builder.Navigation(r => r.Columns)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Soft delete global query filter
        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Column"/>
/// child entity within the RetroBoard aggregate.
/// </summary>
public class ColumnConfiguration : IEntityTypeConfiguration<Column>
{
    /// <summary>Configures the Column entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Column> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(c => c.Notes)
            .WithOne()
            .HasForeignKey(n => n.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF Core to use the backing field for the Notes navigation
        builder.Navigation(c => c.Notes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unique index: column names must be unique within a retro board
        // Uses filter to exclude soft-deleted records so names can be reused
        builder.HasIndex(c => new { c.RetroBoardId, c.Name })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Soft delete global query filter
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Note"/>
/// child entity within the RetroBoard aggregate.
/// </summary>
public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    /// <summary>Configures the Note entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Text)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasMany(n => n.Votes)
            .WithOne()
            .HasForeignKey(v => v.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF Core to use the backing field for the Votes navigation
        builder.Navigation(n => n.Votes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unique index: note text must be unique within a column
        // Uses filter to exclude soft-deleted records so text can be reused
        builder.HasIndex(n => new { n.ColumnId, n.Text })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Soft delete global query filter
        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Vote"/>
/// child entity within the RetroBoard aggregate.
/// </summary>
/// <remarks>
/// DESIGN: The unique index on (NoteId, UserId) acts as a database-level
/// safety net for the one-vote-per-user-per-note invariant. In API 3,
/// the aggregate root's <see cref="RetroBoard.CastVote"/> method enforces
/// this in-memory, and the xmin concurrency token prevents race conditions.
/// The DB constraint is a defence-in-depth measure.
/// </remarks>
public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    /// <summary>Configures the Vote entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasKey(v => v.Id);

        // Unique index: one vote per user per note
        // Uses filter to exclude soft-deleted records so votes can be re-cast
        builder.HasIndex(v => new { v.NoteId, v.UserId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Soft delete global query filter
        builder.HasQueryFilter(v => v.DeletedAt == null);
    }
}
