using EnterpriseTaskManagement.Application.Features.Tasks.Common;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.DeleteTask;

/// <summary>Soft-deletes a task, addressed by PublicId.</summary>
public sealed record DeleteTaskCommand(
    Guid PublicId,
    Guid RequesterPublicId,
    TaskRequesterRole RequesterRole) : IRequest<DeleteTaskResponse>;
