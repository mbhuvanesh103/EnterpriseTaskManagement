using EnterpriseTaskManagement.Application.Common.Models.Dashboard;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetUserDashboard;

/// <summary>Explicit response contract of <c>GetUserDashboardQuery</c>.</summary>
public sealed record GetUserDashboardResponse(
    DashboardUserMetrics People,
    string Scope,
    DateTimeOffset GeneratedAtUtc);
