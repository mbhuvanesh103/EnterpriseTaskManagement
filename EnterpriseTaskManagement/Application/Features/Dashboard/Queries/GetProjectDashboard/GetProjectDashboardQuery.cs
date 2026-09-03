using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetProjectDashboard;

/// <summary>
/// Reads the project-focused dashboard. An Admin sees every project; a Manager
/// only the projects they lead; an Employee is refused (403).
/// </summary>
public sealed record GetProjectDashboardQuery(
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole) : IRequest<GetProjectDashboardResponse>;
