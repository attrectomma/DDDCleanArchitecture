using Api5.Domain.RetroAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api5.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Note"/>
/// child entity within the RetroBoard aggregate.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 4. Note has no Votes navigation because
/// Vote is its own aggregate. The Note → Vote relationship is configured
/// from the Vote side in <see cref="VoteConfiguration"/>.
/// </remarks>
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

        // ❌ No HasMany(n => n.Votes) — Vote is a separate aggregate
        // The relationship is configured from the Vote side in VoteConfiguration.

        builder.HasIndex(n => new { n.ColumnId, n.Text })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasQueryFilter(n => n.DeletedAt == null);
    }
}
