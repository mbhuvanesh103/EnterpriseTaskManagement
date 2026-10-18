namespace EnterpriseTaskManagement.Application.Common.Models.Dashboard;

/// <summary>
/// Task-side dashboard read model. Every number counts only tasks that are not
/// soft-deleted and that fall inside the caller's server-derived scope.
/// </summary>
/// <param name="Total">Tasks in scope.</param>
/// <param name="Completed">Tasks whose status is <c>Completed</c>.</param>
/// <param name="Overdue">
/// Tasks with a due date strictly before the current UTC instant whose status is
/// not <c>Completed</c>.
/// </param>
/// <param name="DueToday">
/// Tasks whose due date falls inside the current UTC calendar day
/// (<c>[today 00:00Z, tomorrow 00:00Z)</c>) and whose status is not <c>Completed</c>.
/// </param>
/// <param name="Unassigned">Tasks in scope with no assignee.</param>
/// <param name="ByStatus">Counts per <c>TaskItemStatus</c> name.</param>
/// <param name="ByPriority">Counts per <c>TaskPriority</c> name.</param>
public sealed record DashboardTaskMetrics(
    int Total,
    int Completed,
    int Overdue,
    int DueToday,
    int Unassigned,
    IReadOnlyList<DashboardCount> ByStatus,
    IReadOnlyList<DashboardCount> ByPriority);
