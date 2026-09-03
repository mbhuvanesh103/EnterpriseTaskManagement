using EnterpriseTaskManagement.Application.Common.Models.Dashboard;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetTaskDashboard;

/// <summary>Explicit response contract of <c>GetTaskDashboardQuery</c>.</summary>
public sealed record GetTaskDashboardResponse(
    DashboardTaskMetrics Tasks,
    string Scope,
    DateTimeOffset GeneratedAtUtc);
