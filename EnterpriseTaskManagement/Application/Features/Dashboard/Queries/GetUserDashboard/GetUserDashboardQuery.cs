using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetUserDashboard;

/// <summary>
/// Reads the organization-wide people dashboard. Admin only; any other role is
/// refused (403).
/// </summary>
public sealed record GetUserDashboardQuery(
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole) : IRequest<GetUserDashboardResponse>;
