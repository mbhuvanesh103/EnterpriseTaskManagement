using EnterpriseTaskManagement.Application.Common.Models.Dashboard;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetProjectDashboard;

/// <summary>Explicit response contract of <c>GetProjectDashboardQuery</c>.</summary>
public sealed record GetProjectDashboardResponse(
    DashboardProjectMetrics Projects,
    string Scope,
    DateTimeOffset GeneratedAtUtc);
