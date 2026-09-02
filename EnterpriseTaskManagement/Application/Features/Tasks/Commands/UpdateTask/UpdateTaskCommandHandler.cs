using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Tasks;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.UpdateTask;

/// <summary>
/// Loads the aggregate by PublicId, enforces the project-manager ownership rule,
/// then applies the descriptive changes through the aggregate's behaviour.
/// Concurrent writes are rejected by the rowversion token and surface as 409.
/// </summary>
public sealed class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskSummary>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTaskCommandHandler(
        ITaskRepository tasks,
        IProjectRepository projects,
        IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskSummary> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        TaskItem task = await _tasks.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(TaskItem), request.PublicId, "public id");

        _ = await TaskAuthorization.EnsureCanManageAsync(
            _projects,
            task.ProjectId,
            request.RequesterPublicId,
            request.RequesterRole,
            cancellationToken);

        task.Update(
            request.Title,
            request.Description,
            ParsePriority(request.Priority, task.Priority),
            request.DueDateUtc);

        _tasks.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskSummary.FromTask(task);
    }

    private static TaskPriority ParsePriority(string? priority, TaskPriority current) =>
        priority is null
            ? current
            : Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsed)
                ? parsed
                : throw new ArgumentException(
                    "Task priority must be one of: Low, Medium, High, Critical.",
                    nameof(priority));
}
