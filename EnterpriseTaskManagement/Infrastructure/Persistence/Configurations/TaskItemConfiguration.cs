using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Tasks;
using EnterpriseTaskManagement.Domain.Entities.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="TaskItem"/> aggregate to the <c>etm.Tasks</c> table. All
/// mapping knowledge lives here so the Domain needs no EF attributes. Guids are
/// persisted as 32-char "N" strings exactly like Users, Departments and Projects;
/// the relationship to Project uses <c>Restrict</c> and the optional assignee
/// relationship to User uses <c>NoAction</c>, so a referenced row can never be
/// removed underneath a task and SQL Server needs no multi-cascade paths.
/// </summary>
public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.PublicId)
            .HasConversion(id => id.ToString("N"), value => Guid.ParseExact(value, "N"))
            .HasColumnName("PublicId")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(t => t.PublicId).IsUnique();

        builder.Property(t => t.Title)
            .HasColumnName("Title")
            .HasMaxLength(TaskItem.MaxTitleLength)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("Description")
            .HasMaxLength(TaskItem.MaxDescriptionLength);

        // Owning project, referenced by Project.PublicId.
        builder.Property(t => t.ProjectId)
            .HasConversion(id => id.ToString("N"), value => Guid.ParseExact(value, "N"))
            .HasColumnName("ProjectId")
            .HasMaxLength(32)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .HasPrincipalKey(p => p.PublicId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(t => t.ProjectId);

        // Optional assignee, referenced by User.PublicId.
        builder.Property(t => t.AssignedUserId)
            .HasConversion(
                id => id.HasValue ? id.Value.ToString("N") : null,
                value => value == null ? null : Guid.ParseExact(value, "N"))
            .HasColumnName("AssignedUserId")
            .HasMaxLength(32)
            .IsUnicode(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.AssignedUserId)
            .HasPrincipalKey(u => u.PublicId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder.HasIndex(t => t.AssignedUserId);

        // Priority and workflow state: stored as readable names, required, with a
        // safe default so existing rows can never carry an unknown value.
        builder.Property(t => t.Priority)
            .HasConversion<string>()
            .HasColumnName("Priority")
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasColumnName("Status")
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired()
            .HasDefaultValue(TaskItemStatus.Todo);

        builder.HasIndex(t => t.Status);

        builder.Property(t => t.DueDateUtc);
        builder.Property(t => t.CompletedAtUtc);

        builder.Ignore(t => t.IsOpen);
        builder.Ignore(t => t.IsAssigned);

        // Auditing.
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.ModifiedAtUtc);
        builder.Property(t => t.ModifiedBy).HasMaxLength(256);

        // Soft deletion + global query filter.
        builder.Property(t => t.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(t => t.DeletedAtUtc);
        builder.Property(t => t.DeletedBy).HasMaxLength(256);
        builder.HasQueryFilter(t => !t.IsDeleted);

        // Optimistic concurrency: SQL Server rowversion owned by the database.
        builder.Property(t => t.Version)
            .IsRowVersion()
            .HasColumnName("Version");

        builder.Ignore(t => t.DomainEvents);
        builder.Ignore(t => t.IsTransient);
    }
}
