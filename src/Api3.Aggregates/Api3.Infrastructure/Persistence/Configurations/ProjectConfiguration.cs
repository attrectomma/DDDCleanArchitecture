using Api3.Domain.ProjectAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api3.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Project"/> aggregate root
/// and its child entity <see cref="ProjectMember"/>.
/// </summary>
/// <remarks>
/// DESIGN: In API 3, the Project aggregate root uses
/// <see cref="NpgsqlEntityTypeBuilderExtensions.UseXminAsConcurrencyToken"/>
/// for optimistic concurrency. This means any modification to the Project
/// (including adding/removing members) will bump the xmin, and concurrent
/// modifications will throw <c>DbUpdateConcurrencyException</c>.
///
/// The ProjectMember configuration is included here (rather than in a
/// separate file) because it is a child entity within the Project aggregate.
/// This grouping makes the aggregate boundary visible in the infrastructure layer.
/// </remarks>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <summary>Configures the Project entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(300);

        // DESIGN: xmin-based optimistic concurrency for the aggregate root.
        // Any concurrent write to the same Project will conflict.
        // Maps the Version property to PostgreSQL's xmin system column.
        builder.Property(p => p.Version)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(p => p.Members)
            .WithOne()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF Core to use the backing field for the Members navigation
        builder.Navigation(p => p.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Soft delete global query filter
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ProjectMember"/>
/// child entity within the Project aggregate.
/// </summary>
public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    /// <summary>Configures the ProjectMember entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.HasKey(pm => pm.Id);

        // Unique index: a user can only be a member of a project once
        // Uses filter to exclude soft-deleted records so memberships can be re-added
        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Soft delete global query filter
        builder.HasQueryFilter(pm => pm.DeletedAt == null);
    }
}
