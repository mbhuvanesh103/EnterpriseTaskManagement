using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Tasks;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.ChangeTaskStatus;

/// <summary>
/// Admin and the owning project's Manager may move any task in that project; an
/// Employee may only move a task assigned to themselves. Whether the transition
/// itself is legal is decided by the aggregate.
/// </summary>
public sealed class ChangeTaskStatusCommandHandler : IRequestHandler<ChangeTaskStatusCommand, TaskSummary>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeTaskStatusCommandHandler(
        ITaskRepository tasks,
        IProjectRepository projects,
        IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskSummary> Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        TaskItem task = await _tasks.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(TaskItem), request.PublicId, "public id");

        await TaskAuthorization.EnsureCanChangeStatusAsync(
            _projects,
            task,
            request.RequesterPublicId,
            request.RequesterRole,
            cancellationToken);

        task.ChangeStatus(ParseStatus(request.Status));

        _tasks.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskSummary.FromTask(task);
    }

    private static TaskItemStatus ParseStatus(string status) =>
        Enum.TryParse<TaskItemStatus>(status, ignoreCase: true, out var parsed)
            ? parsed
            : throw new ArgumentException(
                "Task status must be one of: Todo, InProgress, Review, Completed, Blocked.",
                nameof(status));
}
