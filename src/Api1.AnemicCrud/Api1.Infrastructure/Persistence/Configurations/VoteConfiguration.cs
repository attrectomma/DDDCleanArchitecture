using Api1.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api1.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Vote"/> entity.
/// </summary>
/// <remarks>
/// DESIGN: The unique index on (NoteId, UserId) acts as a database-level
/// safety net for the one-vote-per-user-per-note invariant. In API 1 the
/// service layer checks this constraint before inserting, but that check
/// is not atomic under concurrency. The DB constraint catches any races
/// that slip through, though the resulting exception is not gracefully handled.
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
