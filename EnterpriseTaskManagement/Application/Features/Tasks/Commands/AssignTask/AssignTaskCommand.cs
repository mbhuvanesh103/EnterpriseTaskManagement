using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.AssignTask;

/// <summary>
/// Assigns (or, with a null <paramref name="AssignedUserId"/>, unassigns) a task.
/// Both the task and the assignee are addressed by public id.
/// </summary>
public sealed record AssignTaskCommand(
    Guid PublicId,
    Guid? AssignedUserId,
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole) : IRequest<TaskSummary>;
