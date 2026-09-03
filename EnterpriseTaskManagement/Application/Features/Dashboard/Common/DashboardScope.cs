using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Common;

/// <summary>
/// The single place the dashboard visibility rules live. It reuses the existing
/// <see cref="TaskRequesterRole"/> role representation — no second role system is
/// introduced — and turns the authenticated caller's identity into explicit query
/// scope. The identity always arrives from the validated JWT via the controller
/// and is never read from client input.
/// <list type="bullet">
/// <item>Admin — unrestricted.</item>
/// <item>Manager — projects they manage, and the tasks inside those projects.</item>
/// <item>Employee — only tasks assigned to themselves; no project or people data.</item>
/// </list>
/// </summary>
internal static class DashboardScope
{
    /// <summary>Scope parameters for the task aggregates.</summary>
    internal static (Guid? ManagerPublicId, Guid? AssigneePublicId) ForTasks(
        Guid requesterPublicId,
        TaskRequesterRole requesterRole) => requesterRole switch
        {
            TaskRequesterRole.Admin => (null, null),
            TaskRequesterRole.Manager => (requesterPublicId, (Guid?)null),
            _ => ((Guid?)null, requesterPublicId),
        };

    /// <summary>
    /// Scope parameter for the project aggregates. An Employee has no project
    /// dashboard at all, so the request is refused rather than silently emptied.
    /// </summary>
    internal static Guid? ForProjects(Guid requesterPublicId, TaskRequesterRole requesterRole) =>
        requesterRole switch
        {
            TaskRequesterRole.Admin => null,
            TaskRequesterRole.Manager => requesterPublicId,
            _ => throw new ForbiddenAccessException(
                "Project dashboard data is available to administrators and project managers only."),
        };

    /// <summary>Organization-wide people data is Admin-only.</summary>
    internal static void EnsureOrganizationWide(TaskRequesterRole requesterRole)
    {
        if (requesterRole != TaskRequesterRole.Admin)
        {
            throw new ForbiddenAccessException(
                "Organization-wide dashboard data is available to administrators only.");
        }
    }
}

/// <summary>
/// The dashboard's time convention, in one place. No clock abstraction exists in
/// the solution today (Phases 1–11 call <c>DateTimeOffset.UtcNow</c> directly, for
/// example in the aggregates' soft-delete paths), so this helper keeps that same
/// convention instead of inventing a new one: everything is UTC, "overdue" is
/// measured against the current UTC instant, and "today" is the current UTC
/// calendar day as the half-open window <c>[00:00Z, next 00:00Z)</c>.
/// </summary>
internal readonly record struct DashboardInstant(
    DateTimeOffset AsOfUtc,
    DateTimeOffset DayStartUtc,
    DateTimeOffset NextDayStartUtc)
{
    internal static DashboardInstant Now()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var dayStart = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);

        return new DashboardInstant(now, dayStart, dayStart.AddDays(1));
    }
}
