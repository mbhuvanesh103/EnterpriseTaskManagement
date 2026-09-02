using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.UpdateTask;

/// <summary>
/// Use-case input for updating a task's descriptive fields, addressed by
/// PublicId. Reassignment and status transitions are deliberately NOT part of
/// this command — they are separate use cases with their own rules.
/// </summary>
public sealed record UpdateTaskCommand(
    Guid PublicId,
    string Title,
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole,
    string? Description = null,
    string? Priority = null,
    DateTimeOffset? DueDateUtc = null) : IRequest<TaskSummary>;
