using Api5.Domain.UserAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api5.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the <see cref="User"/> aggregate root.
/// </summary>
/// <remarks>
/// DESIGN: Identical to API 3/4. User has no concurrency token because it
/// is a simple aggregate with minimal contention.
/// </remarks>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <summary>Configures the User entity mapping.</summary>
    /// <param name="builder">The entity type builder.</param>
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasQueryFilter(u => u.DeletedAt == null);
    }
}
