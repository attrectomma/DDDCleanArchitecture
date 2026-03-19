using Api5.Domain.RetroAggregate;
using Api5.Domain.VoteAggregate.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api5.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="RetroBoard"/> aggregate root
/// and its child entity <see cref="Column"/>.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 4, with the addition of the
/// <see cref="VotingStrategyType"/> column. The RetroBoard aggregate
/// owns columns and notes but not votes. Vote has its own configuration
/// in <see cref="VoteConfiguration"/>.
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

        // DESIGN: Store the voting strategy as a readable string ("Default", "Budget")
        // rather than an integer. This makes the database self-documenting and
        // simplifies debugging.
        builder.Property(r => r.VotingStrategyType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(VotingStrategyType.Default)
            .IsRequired();

        // DESIGN: xmin concurrency token. In API 5 (same as API 4), voting
        // does NOT bump this token because Vote is its own aggregate. The
        // xmin is only checked when the root row itself is UPDATEd. For
        // concurrent child creation, unique constraints are the safety net.
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
