using Api1.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api1.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="RetroBoard"/> entity.
/// </summary>
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

        builder.HasOne(r => r.Project)
            .WithMany(p => p.RetroBoards)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Columns)
            .WithOne(c => c.RetroBoard)
            .HasForeignKey(c => c.RetroBoardId)
            .OnDelete(DeleteBehavior.Cascade);

        // Soft delete global query filter
        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
