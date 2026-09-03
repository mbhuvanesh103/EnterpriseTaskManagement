using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Dashboard.Queries.GetTaskDashboard;

/// <summary>
/// Reads the task-focused dashboard for the authenticated caller: an Admin sees
/// every task, a Manager only tasks in projects they lead, an Employee only tasks
/// assigned to themselves.
/// </summary>
public sealed record GetTaskDashboardQuery(
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole) : IRequest<GetTaskDashboardResponse>;
