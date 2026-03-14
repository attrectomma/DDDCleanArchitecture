using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api2.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Vote"/> entity.
/// </summary>
/// <remarks>
/// DESIGN: The unique index on (NoteId, UserId) acts as a database-level
/// safety net for the one-vote-per-user-per-note invariant. In API 2 the
/// <see cref="Note.CastVote"/> domain method enforces this in-memory, but
/// under concurrent access the check can still be bypassed because there
/// is no aggregate-level concurrency token. The DB constraint catches any
/// races that slip through.
/// </remarks>
public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    /// <summary>Configures the Vote entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Vote> builder)
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
        // Uses filter to exclude soft-deleted records so votes can be re-cast
        builder.HasIndex(v => new { v.NoteId, v.UserId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Soft delete global query filter
        builder.HasQueryFilter(v => v.DeletedAt == null);
    }
}
