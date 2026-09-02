using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Tasks;
using EnterpriseTaskManagement.Domain.Entities.Users;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.CreateTask;

/// <summary>
/// Verifies the caller may manage tasks in the target project (Admin anywhere,
/// Manager only in projects they lead), checks any assignee exists, then creates
/// the aggregate through its own factory and commits.
/// </summary>
public sealed class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskSummary>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTaskCommandHandler(
        ITaskRepository tasks,
        IProjectRepository projects,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _projects = projects;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskSummary> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _ = await TaskAuthorization.EnsureCanManageAsync(
            _projects,
            request.ProjectId,
            request.RequesterPublicId,
            request.RequesterRole,
            cancellationToken);

        if (request.AssignedUserId is { } assignee)
        {
            _ = await _users.GetByPublicIdAsync(assignee, cancellationToken)
                ?? throw NotFoundException.For(nameof(User), assignee, "public id");
        }

        TaskItem task = TaskItem.Create(
            request.Title,
            request.ProjectId,
            request.Description,
            request.AssignedUserId,
            ParsePriority(request.Priority),
            request.DueDateUtc);

        await _tasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskSummary.FromTask(task);
    }

    private static TaskPriority ParsePriority(string? priority) =>
        priority is null
            ? TaskPriority.Medium
            : Enum.TryParse<TaskPriority>(priority, ignoreCase: true, out var parsed)
                ? parsed
                : throw new ArgumentException(
                    "Task priority must be one of: Low, Medium, High, Critical.",
                    nameof(priority));
}
