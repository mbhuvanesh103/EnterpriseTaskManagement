using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="Project"/> aggregate to the <c>etm.Projects</c> table. All
/// mapping knowledge lives here so the Domain needs no EF attributes. Guids are
/// persisted as 32-char "N" strings, exactly like Users and Departments, and the
/// relationships to Department and User use <c>Restrict</c> so a referenced row
/// can never be removed underneath a project.
/// </summary>
public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.PublicId)
            .HasConversion(id => id.ToString("N"), value => Guid.ParseExact(value, "N"))
            .HasColumnName("PublicId")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(p => p.PublicId).IsUnique();

        builder.Property(p => p.Name)
            .HasColumnName("Name")
            .HasMaxLength(Project.MaxNameLength)
            .IsRequired();

        builder.Property(p => p.Code)
            .HasColumnName("Code")
            .HasMaxLength(Project.MaxCodeLength)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Description)
            .HasColumnName("Description")
            .HasMaxLength(Project.MaxDescriptionLength);

        // Owning department, referenced by Department.PublicId.
        builder.Property(p => p.DepartmentId)
            .HasConversion(id => id.ToString("N"), value => Guid.ParseExact(value, "N"))
            .HasColumnName("DepartmentId")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .HasPrincipalKey(d => d.PublicId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(p => p.DepartmentId);

        // Responsible manager, referenced by User.PublicId.
        builder.Property(p => p.ManagerId)
            .HasConversion(id => id.ToString("N"), value => Guid.ParseExact(value, "N"))
            .HasColumnName("ManagerId")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.ManagerId)
            .HasPrincipalKey(u => u.PublicId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired();

        builder.HasIndex(p => p.ManagerId);

        // Lifecycle state: stored as its readable name, required, safe default.
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasColumnName("Status")
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired()
            .HasDefaultValue(ProjectStatus.Planned);

        builder.Property(p => p.StartDateUtc).IsRequired();
        builder.Property(p => p.TargetEndDateUtc);

        builder.Ignore(p => p.IsOpen);

        // Auditing.
        builder.Property(p => p.CreatedAtUtc).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(256);
        builder.Property(p => p.ModifiedAtUtc);
        builder.Property(p => p.ModifiedBy).HasMaxLength(256);

        // Soft deletion + global query filter.
        builder.Property(p => p.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(p => p.DeletedAtUtc);
        builder.Property(p => p.DeletedBy).HasMaxLength(256);
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Optimistic concurrency: SQL Server rowversion owned by the database.
        builder.Property(p => p.Version)
            .IsRowVersion()
            .HasColumnName("Version");

        builder.Ignore(p => p.DomainEvents);
        builder.Ignore(p => p.IsTransient);
    }
}
