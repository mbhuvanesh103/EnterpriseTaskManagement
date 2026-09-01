using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Users;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseTaskManagement.Infrastructure.Persistence;

/// <summary>
/// EF Core 10 / SQL Server unit of work for the whole solution. Mapping lives in
/// <c>Persistence/Configurations</c> (one <see cref="IEntityTypeConfiguration{TEntity}"/>
/// per aggregate) and is discovered from this assembly, so no mapping code and no
/// EF attributes exist in the Domain.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IUnitOfWork
{
    public const string DefaultSchema = "etm";

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Stamps audit values before writing. Soft deletes are expressed by the
    /// domain (<c>MarkDeleted</c>) and enforced by per-entity query filters;
    /// this method only converts a hard <c>Remove</c> into a soft delete.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity<long>>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarkCreated(nowUtc);
                    break;
                case EntityState.Modified:
                    entry.Entity.MarkModified(nowUtc);
                    break;
                case EntityState.Deleted:
                    // Never physically delete auditable aggregates.
                    entry.State = EntityState.Modified;
                    entry.Entity.MarkDeleted(nowUtc);
                    break;
                default:
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
