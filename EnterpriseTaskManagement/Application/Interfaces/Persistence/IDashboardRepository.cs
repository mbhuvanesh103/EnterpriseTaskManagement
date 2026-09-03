using EnterpriseTaskManagement.Application.Common.Models.Dashboard;

namespace EnterpriseTaskManagement.Application.Interfaces.Persistence;

/// <summary>
/// Purpose-built read abstraction for the dashboard. The aggregate repositories
/// return whole entity collections, which would mean loading every row into
/// memory just to count it; this interface instead returns finished read models
/// so the Infrastructure implementation can push <c>COUNT</c> / <c>GROUP BY</c>
/// down to SQL Server. Declared in Application and implemented in Infrastructure:
/// no EF Core type, no <c>IQueryable</c>, and no <c>DbContext</c> ever appears
/// here.
///
/// Scope is always expressed as explicit, server-derived parameters:
/// <list type="bullet">
/// <item><paramref name="managerPublicId"/> — restrict to projects led by this
/// user (and the tasks inside them). Used for the Manager role.</item>
/// <item><paramref name="assigneePublicId"/> — restrict to tasks assigned to this
/// user. Used for the Employee role.</item>
/// </list>
/// Both <c>null</c> means unrestricted (Admin). Soft-deleted rows are excluded
/// from every count.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>
    /// Task counters for the given scope. <paramref name="asOfUtc"/> is the
    /// instant "overdue" is measured against; <paramref name="dayStartUtc"/> and
    /// <paramref name="nextDayStartUtc"/> delimit the half-open "today" window.
    /// </summary>
    Task<DashboardTaskMetrics> GetTaskMetricsAsync(
        Guid? managerPublicId,
        Guid? assigneePublicId,
        DateTimeOffset asOfUtc,
        DateTimeOffset dayStartUtc,
        DateTimeOffset nextDayStartUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Project counters, optionally restricted to one manager's projects.</summary>
    Task<DashboardProjectMetrics> GetProjectMetricsAsync(
        Guid? managerPublicId,
        CancellationToken cancellationToken = default);

    /// <summary>Organization-wide people and department counters (Admin scope only).</summary>
    Task<DashboardUserMetrics> GetUserMetricsAsync(CancellationToken cancellationToken = default);
}
