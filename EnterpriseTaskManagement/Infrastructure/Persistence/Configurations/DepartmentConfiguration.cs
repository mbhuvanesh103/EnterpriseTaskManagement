using EnterpriseTaskManagement.Domain.Entities.Departments;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="Department"/> aggregate to <c>etm.Departments</c>.
/// <c>PublicId</c> is an alternate key because users reference it.
/// </summary>
public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.PublicId)
            .HasConversion(id => id.ToString("N"), value => Guid.ParseExact(value, "N"))
            .HasColumnName("PublicId")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.HasAlternateKey(d => d.PublicId);

        builder.Property(d => d.Name).HasMaxLength(Department.MaxNameLength).IsRequired();

        builder.Property(d => d.Code)
            .HasMaxLength(Department.MaxCodeLength)
            .IsUnicode(false)
            .IsRequired();

        // Unique because the duplicate-code business rule depends on it.
        builder.HasIndex(d => d.Code).IsUnique();

        builder.HasIndex(d => d.PublicId).IsUnique();

        builder.Property(d => d.Description).HasMaxLength(Department.MaxDescriptionLength);

        builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(d => d.CreatedAtUtc).IsRequired();
        builder.Property(d => d.CreatedBy).HasMaxLength(256);
        builder.Property(d => d.ModifiedAtUtc);
        builder.Property(d => d.ModifiedBy).HasMaxLength(256);

        builder.Property(d => d.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(d => d.DeletedAtUtc);
        builder.Property(d => d.DeletedBy).HasMaxLength(256);
        builder.HasQueryFilter(d => !d.IsDeleted);

        builder.Property(d => d.Version)
            .IsRowVersion()
            .HasColumnName("Version");

        builder.Ignore(d => d.DomainEvents);
        builder.Ignore(d => d.IsTransient);
    }
}
