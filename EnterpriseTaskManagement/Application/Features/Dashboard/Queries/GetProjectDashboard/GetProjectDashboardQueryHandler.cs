using EnterpriseTaskManagement.Application.Common.Models.Dashboard;
using EnterpriseTaskManagement.Application.Features.Dashboard.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetProjectDashboard;

/// <summary>Query handler: scope resolution → aggregated repository read → DTO.</summary>
public sealed class GetProjectDashboardQueryHandler
    : IRequestHandler<GetProjectDashboardQuery, GetProjectDashboardResponse>
{
    private readonly IDashboardRepository _dashboard;

    public GetProjectDashboardQueryHandler(IDashboardRepository dashboard) => _dashboard = dashboard;

    public async Task<GetProjectDashboardResponse> Handle(
        GetProjectDashboardQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var instant = DashboardInstant.Now();
        Guid? managerPublicId =
            DashboardScope.ForProjects(request.RequesterPublicId, request.RequesterRole);

        DashboardProjectMetrics projects =
            await _dashboard.GetProjectMetricsAsync(managerPublicId, cancellationToken);

        return new GetProjectDashboardResponse(
            projects,
            request.RequesterRole.ToString(),
            instant.AsOfUtc);
    }
}
