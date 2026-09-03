namespace EnterpriseTaskManagement.Application.Common.Models.Dashboard;

/// <summary>
/// One grouped dashboard bucket: the group key as its Domain name (for example
/// <c>Active</c>, <c>Todo</c>, <c>Critical</c>, <c>Manager</c>) and the number of
/// non-deleted rows in it. Keys are produced from the Domain enums, so the exact
/// existing vocabulary is preserved and no arbitrary strings are invented.
/// </summary>
public sealed record DashboardCount(string Key, int Count);
