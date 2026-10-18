namespace EnterpriseTaskManagement.Application.Common.Models.Dashboard;

/// <summary>
/// Organization-wide people read model, available to an Admin only. It carries
/// counts and Domain role names — never names, e-mail addresses, employee codes,
/// password hashes, or internal database identifiers.
/// </summary>
/// <param name="TotalUsers">Users that are not soft-deleted.</param>
/// <param name="ActiveUsers">Non-deleted users whose <c>IsActive</c> is true.</param>
/// <param name="InactiveUsers">Non-deleted users whose <c>IsActive</c> is false.</param>
/// <param name="ByRole">Counts per <c>UserRole</c> name (Employee / Manager / Admin).</param>
/// <param name="TotalDepartments">Departments that are not soft-deleted.</param>
/// <param name="ActiveDepartments">Non-deleted departments whose <c>IsActive</c> is true.</param>
public sealed record DashboardUserMetrics(
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers,
    IReadOnlyList<DashboardCount> ByRole,
    int TotalDepartments,
    int ActiveDepartments);
