using EnterpriseTaskManagement.Application.Common.Models.Dashboard;
using EnterpriseTaskManagement.Application.Features.Dashboard.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetUserDashboard;

/// <summary>Query handler: Admin assertion → aggregated repository read → DTO.</summary>
public sealed class GetUserDashboardQueryHandler
    : IRequestHandler<GetUserDashboardQuery, GetUserDashboardResponse>
{
    private readonly IDashboardRepository _dashboard;

    public GetUserDashboardQueryHandler(IDashboardRepository dashboard) => _dashboard = dashboard;

    public async Task<GetUserDashboardResponse> Handle(
        GetUserDashboardQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        DashboardScope.EnsureOrganizationWide(request.RequesterRole);

        var instant = DashboardInstant.Now();
        DashboardUserMetrics people = await _dashboard.GetUserMetricsAsync(cancellationToken);

        return new GetUserDashboardResponse(
            people,
            request.RequesterRole.ToString(),
            instant.AsOfUtc);
    }
}
