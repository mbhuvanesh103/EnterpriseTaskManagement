using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="User"/> aggregate to the <c>etm.Users</c> table. All
/// mapping knowledge lives here so the Domain needs no EF attributes.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        // Guid persisted as a 32-char "N" string, as required.
        builder.Property(u => u.PublicId)
            .HasConversion(id => id.ToString("N"), value => Guid.ParseExact(value, "N"))
            .HasColumnName("PublicId")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(u => u.PublicId).IsUnique();

        // FullName: owned value object, one column per part plus the normalized value.
        builder.OwnsOne(u => u.Name, name =>
        {
            name.Property(n => n.FirstName).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
            name.Property(n => n.MiddleName).HasColumnName("MiddleName").HasMaxLength(100);
            name.Property(n => n.LastName).HasColumnName("LastName").HasMaxLength(100).IsRequired();
            name.Property(n => n.Value).HasColumnName("FullName").HasMaxLength(FullName.MaxLength).IsRequired();
            name.HasIndex(n => n.Value);
        });
        builder.Navigation(u => u.Name).IsRequired();

        // Email / EmployeeCode: single-value objects mapped through converters so
        // the Domain factories keep owning validation and normalization.
        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasColumnName("Email")
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.EmployeeCode)
            .HasConversion(code => code.Value, value => EmployeeCode.Create(value))
            .HasColumnName("EmployeeCode")
            .HasMaxLength(EmployeeCode.Length)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(u => u.EmployeeCode).IsUnique();

        // Required relationship to Department via its 32-char PublicId.
        builder.Property(u => u.DepartmentId)
            .HasConversion(id => id.ToString("N"), value => Guid.ParseExact(value, "N"))
            .HasColumnName("DepartmentId")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(u => u.DepartmentId)
            .HasPrincipalKey(d => d.PublicId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(u => u.DepartmentId);

        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);

        // Auditing.
        builder.Property(u => u.CreatedAtUtc).IsRequired();
        builder.Property(u => u.CreatedBy).HasMaxLength(256);
        builder.Property(u => u.ModifiedAtUtc);
        builder.Property(u => u.ModifiedBy).HasMaxLength(256);

        // Soft deletion + global query filter.
        builder.Property(u => u.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(u => u.DeletedAtUtc);
        builder.Property(u => u.DeletedBy).HasMaxLength(256);
        builder.HasQueryFilter(u => !u.IsDeleted);

        // Optimistic concurrency: SQL Server rowversion owned by the database.
        builder.Property(u => u.Version)
            .IsRowVersion()
            .HasColumnName("Version");

        // Domain events are never persisted.
        builder.Ignore(u => u.DomainEvents);
        builder.Ignore(u => u.IsTransient);
    }
}
