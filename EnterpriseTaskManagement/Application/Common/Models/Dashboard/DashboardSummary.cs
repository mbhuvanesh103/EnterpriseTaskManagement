namespace EnterpriseTaskManagement.Application.Common.Models.Dashboard;

/// <summary>
/// Headline counters for the landing dashboard. Sections the caller is not
/// entitled to are <c>null</c> rather than zero, so a scoped caller can never
/// mistake "hidden" for "none": an Employee receives task figures only, a Manager
/// receives project and task figures for the projects they lead, and only an
/// Admin receives the organization-wide people figures.
/// </summary>
public sealed record DashboardSummary(
    int? ActiveUsers,
    int? ActiveDepartments,
    int? TotalProjects,
    int? ActiveProjects,
    int TotalTasks,
    int CompletedTasks,
    int OverdueTasks,
    int DueTodayTasks);
