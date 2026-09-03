namespace EnterpriseTaskManagement.Application.Common.Models.Dashboard;

/// <summary>
/// Project-side dashboard read model. Counts exclude soft-deleted projects and
/// respect the caller's server-derived scope (a Manager only sees the projects
/// they lead).
/// </summary>
/// <param name="Total">Projects in scope.</param>
/// <param name="Active">Projects whose status is <c>Active</c>.</param>
/// <param name="ByStatus">
/// Counts per <c>ProjectStatus</c> name — Planned / Active / OnHold / Completed /
/// Cancelled, exactly as the Domain defines them.
/// </param>
public sealed record DashboardProjectMetrics(
    int Total,
    int Active,
    IReadOnlyList<DashboardCount> ByStatus);
