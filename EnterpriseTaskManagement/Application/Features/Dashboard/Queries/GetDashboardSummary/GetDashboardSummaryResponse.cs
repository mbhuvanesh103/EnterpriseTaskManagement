using EnterpriseTaskManagement.Application.Common.Models.Dashboard;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetDashboardSummary;

/// <summary>Explicit response contract of <c>GetDashboardSummaryQuery</c>.</summary>
public sealed record GetDashboardSummaryResponse(
    DashboardSummary Summary,
    string Scope,
    DateTimeOffset GeneratedAtUtc);
