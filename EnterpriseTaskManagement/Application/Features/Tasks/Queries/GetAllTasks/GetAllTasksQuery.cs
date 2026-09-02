using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetAllTasks;

/// <summary>
/// Reads tasks that are not soft-deleted. The result is always narrowed by the
/// caller's role: an Admin sees everything, a Manager only tasks in projects they
/// lead, and an Employee only tasks assigned to themselves. The optional filters
/// can narrow further but can never widen that scope.
/// </summary>
public sealed record GetAllTasksQuery(
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole,
    Guid? ProjectId = null,
    Guid? AssignedUserId = null) : IRequest<GetAllTasksResponse>;
