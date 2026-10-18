using EnterpriseTaskManagement.Application.Common.Models.Dashboard;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Tasks;
using EnterpriseTaskManagement.Domain.Entities.Users;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDashboardRepository"/>. Every figure is
/// computed by SQL Server: the queries are <c>COUNT</c> / <c>GROUP BY</c>
/// projections, are <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>
/// (nothing is tracked, nothing is materialized beyond the aggregate rows), and
/// never leave this class as <see cref="IQueryable{T}"/>. Soft-deleted rows are
/// excluded automatically by the global query filters configured in the entity
/// configurations, so no dashboard count can ever include a deleted row.
/// </summary>
public sealed class DashboardRepository : IDashboardRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<DashboardTaskMetrics> GetTaskMetricsAsync(
        Guid? managerPublicId,
        Guid? assigneePublicId,
        DateTimeOffset asOfUtc,
        DateTimeOffset dayStartUtc,
        DateTimeOffset nextDayStartUtc,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TaskItem> tasks = ScopedTasks(managerPublicId, assigneePublicId);

        // One round trip, one row: SQL COUNT / conditional COUNT over the scope.
        var totals = await tasks
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Completed = group.Count(t => t.Status == TaskItemStatus.Completed),
                Overdue = group.Count(t =>
                    t.DueDateUtc != null
                    && t.DueDateUtc < asOfUtc
                    && t.Status != TaskItemStatus.Completed),
                DueToday = group.Count(t =>
                    t.DueDateUtc != null
                    && t.DueDateUtc >= dayStartUtc
                    && t.DueDateUtc < nextDayStartUtc
                    && t.Status != TaskItemStatus.Completed),
                Unassigned = group.Count(t => t.AssignedUserId == null),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var byStatus = await tasks
            .GroupBy(t => t.Status)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var byPriority = await tasks
            .GroupBy(t => t.Priority)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return new DashboardTaskMetrics(
            totals?.Total ?? 0,
            totals?.Completed ?? 0,
            totals?.Overdue ?? 0,
            totals?.DueToday ?? 0,
            totals?.Unassigned ?? 0,
            Buckets<TaskItemStatus>(byStatus.Select(row => (row.Key, row.Count))),
            Buckets<TaskPriority>(byPriority.Select(row => (row.Key, row.Count))));
    }

    public async Task<DashboardProjectMetrics> GetProjectMetricsAsync(
        Guid? managerPublicId,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Project> projects = _dbContext.Projects.AsNoTracking();

        if (managerPublicId is { } manager)
        {
            projects = projects.Where(p => p.ManagerId == manager);
        }

        var totals = await projects
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Active = group.Count(p => p.Status == ProjectStatus.Active),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var byStatus = await projects
            .GroupBy(p => p.Status)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        return new DashboardProjectMetrics(
            totals?.Total ?? 0,
            totals?.Active ?? 0,
            Buckets<ProjectStatus>(byStatus.Select(row => (row.Key, row.Count))));
    }

    public async Task<DashboardUserMetrics> GetUserMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = _dbContext.Users.AsNoTracking();

        var userTotals = await users
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Active = group.Count(u => u.IsActive),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var byRole = await users
            .GroupBy(u => u.Role)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        IQueryable<Department> departments = _dbContext.Departments.AsNoTracking();

        var departmentTotals = await departments
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Total = group.Count(),
                Active = group.Count(d => d.IsActive),
            })
            .FirstOrDefaultAsync(cancellationToken);

        int totalUsers = userTotals?.Total ?? 0;
        int activeUsers = userTotals?.Active ?? 0;

        return new DashboardUserMetrics(
            totalUsers,
            activeUsers,
            totalUsers - activeUsers,
            Buckets<UserRole>(byRole.Select(row => (row.Key, row.Count))),
            departmentTotals?.Total ?? 0,
            departmentTotals?.Active ?? 0);
    }

    /// <summary>
    /// Applies the caller's scope. Manager scope is resolved server-side through
    /// the projects they lead, exactly as <c>TaskRepository</c> does.
    /// </summary>
    private IQueryable<TaskItem> ScopedTasks(Guid? managerPublicId, Guid? assigneePublicId)
    {
        IQueryable<TaskItem> tasks = _dbContext.Tasks.AsNoTracking();

        if (managerPublicId is { } manager)
        {
            tasks = tasks.Where(t => _dbContext.Projects
                .Where(p => p.ManagerId == manager)
                .Select(p => p.PublicId)
                .Contains(t.ProjectId));
        }

        if (assigneePublicId is { } assignee)
        {
            tasks = tasks.Where(t => t.AssignedUserId == assignee);
        }

        return tasks;
    }

    /// <summary>
    /// Projects grouped rows onto the full closed set of Domain enum names, so a
    /// bucket with no rows is reported as zero instead of being silently absent.
    /// </summary>
    private static IReadOnlyList<DashboardCount> Buckets<TEnum>(
        IEnumerable<(TEnum Key, int Count)> rows)
        where TEnum : struct, Enum
    {
        Dictionary<TEnum, int> counts = rows.ToDictionary(row => row.Key, row => row.Count);

        return Enum.GetValues<TEnum>()
            .Select(value => new DashboardCount(
                value.ToString(),
                counts.TryGetValue(value, out int count) ? count : 0))
            .ToList();
    }
}
