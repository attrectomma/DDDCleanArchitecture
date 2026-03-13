using Api1.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api1.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Column"/> entity.
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

        builder.HasOne(c => c.RetroBoard)
            .WithMany(r => r.Columns)
            .HasForeignKey(c => c.RetroBoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Notes)
            .WithOne(n => n.Column)
            .HasForeignKey(n => n.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index: column names must be unique within a retro board
        // Uses filter to exclude soft-deleted records so names can be reused
        builder.HasIndex(c => new { c.RetroBoardId, c.Name })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Soft delete global query filter
        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
