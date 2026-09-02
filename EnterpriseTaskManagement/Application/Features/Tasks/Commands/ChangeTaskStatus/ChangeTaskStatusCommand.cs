using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;

/// <summary>
/// Moves a task to another workflow state. <c>Status</c> is the Domain enum name
/// (Todo, InProgress, Review, Completed, Blocked).
/// </summary>
public sealed record ChangeTaskStatusCommand(
    Guid PublicId,
    string Status,
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole) : IRequest<TaskSummary>;
