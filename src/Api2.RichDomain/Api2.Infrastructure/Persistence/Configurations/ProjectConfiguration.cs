using Api2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api2.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="Project"/> entity.
/// </summary>
/// <remarks>
/// DESIGN: In API 2, the <see cref="Project.Members"/> collection uses a
/// private backing field (<c>_members</c>) with a read-only public property.
/// The <c>UsePropertyAccessMode(PropertyAccessMode.Field)</c> call tells
/// EF Core to use the backing field for materialisation and change tracking,
/// enabling domain methods like <see cref="Project.AddMember"/> and
/// <see cref="Project.RemoveMember"/> to modify the collection while
/// EF Core tracks the changes.
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

        builder.HasMany(p => p.RetroBoards)
            .WithOne(r => r.Project)
            .HasForeignKey(r => r.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Members)
            .WithOne(m => m.Project)
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF Core to use the backing field for the Members navigation
        builder.Navigation(p => p.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Soft delete global query filter
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
