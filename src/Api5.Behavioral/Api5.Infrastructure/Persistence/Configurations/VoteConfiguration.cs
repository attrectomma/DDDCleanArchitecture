using Api5.Domain.RetroAggregate;
using Api5.Domain.VoteAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api5.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures Vote as a standalone aggregate root with its own
/// concurrency token and an index on (NoteId, UserId).
/// </summary>
/// <remarks>
/// DESIGN: The unique constraint on <c>(NoteId, UserId)</c> from API 4 has been
/// replaced with a non-unique index. This is necessary because the
/// <see cref="Api5.Domain.VoteAggregate.Strategies.BudgetVotingStrategy"/>
/// allows multiple votes per user per note ("dot voting").
///
/// The <see cref="Api5.Domain.VoteAggregate.Strategies.DefaultVotingStrategy"/>
/// enforces uniqueness at the application level via the
/// <c>UniqueVotePerNoteSpecification</c>. Without the DB constraint, the
/// Default strategy is susceptible to a rare race condition under high
/// concurrency where two simultaneous requests both pass the <c>ExistsAsync</c>
/// check. In production, mitigate this with serializable transactions or
/// advisory locks. This trade-off is the price of configurable voting strategies
/// and is a valuable educational discussion point.
/// </remarks>
public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    /// <summary>Configures the Vote entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasKey(v => v.Id);

        // DESIGN: Vote has its own xmin concurrency token because
        // it is an aggregate root. Each vote row is independently versioned.
        builder.Property(v => v.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // DESIGN: Non-unique index for query performance on (NoteId, UserId).
        // Previously a unique constraint in API 4, now non-unique to support
        // the BudgetVotingStrategy which allows multiple votes per user per note.
        // See VoteConfiguration class remarks for the full trade-off discussion.
        builder.HasIndex(v => new { v.NoteId, v.UserId })
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
