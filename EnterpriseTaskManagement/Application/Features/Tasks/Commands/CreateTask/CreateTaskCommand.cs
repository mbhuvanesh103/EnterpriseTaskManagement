using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Use-case input for creating a task. Primitives and public ids only;
/// <c>Priority</c> is the Domain enum name (Low, Medium, High, Critical). The
/// requester identity and role come from the authenticated token, never the body.
/// </summary>
public sealed record CreateTaskCommand(
    string Title,
    Guid ProjectId,
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole,
    string? Description = null,
    Guid? AssignedUserId = null,
    string? Priority = null,
    DateTimeOffset? DueDateUtc = null) : IRequest<TaskSummary>;
