using Api4.Domain.RetroAggregate;
using Api4.Domain.VoteAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api4.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures Vote as a standalone aggregate root with its own
/// concurrency token and a unique constraint on (NoteId, UserId).
/// </summary>
/// <remarks>
/// DESIGN: The unique index is the ultimate guarantee of the
/// "one vote per user per note" invariant. Even if the application-level
/// check in VoteService races, this constraint prevents duplicates.
/// This is the "last line of defense" pattern common when splitting aggregates.
///
/// In API 3, Vote's configuration was bundled with the RetroBoard aggregate
/// configurations and did not have its own xmin concurrency token.
/// Now Vote is a first-class aggregate root with:
///   - Its own xmin concurrency token.
///   - A unique index on (NoteId, UserId) as the invariant safety net.
///   - A FK to Note for referential integrity (cascade delete).
/// </remarks>
public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    /// <summary>Configures the Vote entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasKey(v => v.Id);

        // DESIGN: Vote now has its own xmin concurrency token because
        // it is an aggregate root. Each vote row is independently versioned.
        builder.Property(v => v.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Unique index: one vote per user per note — the DB safety net
        builder.HasIndex(v => new { v.NoteId, v.UserId })
            .IsUnique()
            .HasDatabaseName("IX_Vote_NoteId_UserId")
            .HasFilter("\"DeletedAt\" IS NULL");

        // FK to Note — cascade delete so votes are removed when a note is deleted
        builder.HasOne<Note>()
            .WithMany()
            .HasForeignKey(v => v.NoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(v => v.DeletedAt == null);
    }
}
