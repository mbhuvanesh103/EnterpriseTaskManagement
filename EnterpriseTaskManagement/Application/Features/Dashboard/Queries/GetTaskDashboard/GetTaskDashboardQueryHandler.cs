using EnterpriseTaskManagement.Application.Common.Models.Dashboard;
using EnterpriseTaskManagement.Application.Features.Dashboard.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetTaskDashboard;

/// <summary>Query handler: scope resolution → aggregated repository read → DTO.</summary>
public sealed class GetTaskDashboardQueryHandler
    : IRequestHandler<GetTaskDashboardQuery, GetTaskDashboardResponse>
{
    private readonly IDashboardRepository _dashboard;

    public GetTaskDashboardQueryHandler(IDashboardRepository dashboard) => _dashboard = dashboard;

    public async Task<GetTaskDashboardResponse> Handle(
        GetTaskDashboardQuery request,
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

        return new GetTaskDashboardResponse(tasks, request.RequesterRole.ToString(), instant.AsOfUtc);
    }
}
