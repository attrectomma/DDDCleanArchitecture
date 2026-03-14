using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api2.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="ProjectMember"/> join entity.
/// </summary>
public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    /// <summary>Configures the ProjectMember entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.HasKey(pm => pm.Id);

        builder.HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMemberships)
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index: a user can only be a member of a project once
        // Uses filter to exclude soft-deleted records so names can be reused
        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique()
            .HasFilter("\"DeletedAt\" IS NULL");

        // Soft delete global query filter
        builder.HasQueryFilter(pm => pm.DeletedAt == null);
    }
}
