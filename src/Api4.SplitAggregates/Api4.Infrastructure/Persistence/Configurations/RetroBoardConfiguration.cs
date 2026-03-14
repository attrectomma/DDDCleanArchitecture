using Api4.Domain.RetroAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api4.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="RetroBoard"/> aggregate root
/// and its child entity <see cref="Column"/>.
/// </summary>
/// <remarks>
/// DESIGN: Compared to API 3, the RetroBoard configuration is simpler because
/// the aggregate no longer includes Vote. The Column → Note → Vote navigation
/// chain is broken — Note no longer has a Votes collection. Vote has its own
/// configuration in <see cref="VoteConfiguration"/>.
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

        // DESIGN: xmin concurrency token. In API 4, voting does NOT bump
        // this token because Vote is its own aggregate. Only column and
        // note operations on the same retro board will conflict.
        builder.Property(r => r.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(r => r.Columns)
            .WithOne()
            .HasForeignKey(c => c.RetroBoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(r => r.Columns)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

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

        builder.Navigation(c => c.Notes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(c => new { c.RetroBoardId, c.Name })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        builder.HasQueryFilter(c => c.DeletedAt == null);
    }
}
