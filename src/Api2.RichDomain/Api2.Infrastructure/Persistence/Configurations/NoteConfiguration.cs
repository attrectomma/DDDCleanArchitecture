using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api2.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Note"/> entity.
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

        builder.HasOne(n => n.Column)
            .WithMany(c => c.Notes)
            .HasForeignKey(n => n.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(n => n.Votes)
            .WithOne(v => v.Note)
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
