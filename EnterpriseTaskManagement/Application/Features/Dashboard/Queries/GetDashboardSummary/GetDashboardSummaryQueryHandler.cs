using EnterpriseTaskManagement.Application.Common.Models.Dashboard;
using EnterpriseTaskManagement.Application.Features.Dashboard.Common;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetDashboardSummary;

/// <summary>
/// Query handler: scope resolution → aggregated repository reads → DTO. No EF
/// Core, no <c>DbContext</c>, no SQL. Sections outside the caller's entitlement
/// are left <c>null</c> and their queries are never executed.
/// </summary>
public sealed class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, GetDashboardSummaryResponse>
{
    private readonly IDashboardRepository _dashboard;

    public GetDashboardSummaryQueryHandler(IDashboardRepository dashboard) => _dashboard = dashboard;

    public async Task<GetDashboardSummaryResponse> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var instant = DashboardInstant.Now();
        var (managerPublicId, assigneePublicId) =
            DashboardScope.ForTasks(request.RequesterPublicId, request.RequesterRole);

        DashboardTaskMetrics tasks = await _dashboard.GetTaskMetricsAsync(
            managerPublicId,
            assigneePublicId,
            instant.AsOfUtc,
            instant.DayStartUtc,
            instant.NextDayStartUtc,
            cancellationToken);

        DashboardProjectMetrics? projects = request.RequesterRole == TaskRequesterRole.Employee
            ? null
            : await _dashboard.GetProjectMetricsAsync(managerPublicId, cancellationToken);

        DashboardUserMetrics? people = request.RequesterRole == TaskRequesterRole.Admin
            ? await _dashboard.GetUserMetricsAsync(cancellationToken)
            : null;

        var summary = new DashboardSummary(
            people?.ActiveUsers,
            people?.ActiveDepartments,
            projects?.Total,
            projects?.Active,
            tasks.Total,
            tasks.Completed,
            tasks.Overdue,
            tasks.DueToday);

        return new GetDashboardSummaryResponse(
            summary,
            request.RequesterRole.ToString(),
            instant.AsOfUtc);
    }
}
