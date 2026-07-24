using EnterpriseTaskManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PublicId)
            .IsRequired()
            .HasMaxLength(26);

        builder.Property(x => x.DepartmentId)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        // FullName Value Object
        builder.OwnsOne(x => x.Name, name =>
        {
            name.Property(n => n.FirstName)
                .HasColumnName("FirstName")
                .HasMaxLength(100)
                .IsRequired();

            name.Property(n => n.LastName)
                .HasColumnName("LastName")
                .HasMaxLength(100)
                .IsRequired();
        });

        // Email Value Object
        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(255)
                .IsRequired();
        });

        // EmployeeCode Value Object
        builder.OwnsOne(x => x.EmployeeCode, employeeCode =>
        {
            employeeCode.Property(c => c.Value)
                .HasColumnName("EmployeeCode")
                .HasMaxLength(20)
                .IsRequired();
        });
    }
}