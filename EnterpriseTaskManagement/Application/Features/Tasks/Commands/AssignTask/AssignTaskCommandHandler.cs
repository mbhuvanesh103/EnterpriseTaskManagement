using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Tasks;
using EnterpriseTaskManagement.Domain.Entities.Users;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.AssignTask;

/// <summary>
/// Enforces the project-manager ownership rule (Employees may never assign),
/// verifies the target user exists, then delegates to the aggregate's
/// <c>AssignTo</c>/<c>Unassign</c> behaviour.
/// </summary>
public sealed class AssignTaskCommandHandler : IRequestHandler<AssignTaskCommand, TaskSummary>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public AssignTaskCommandHandler(
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

    public async Task<TaskSummary> Handle(AssignTaskCommand request, CancellationToken cancellationToken)
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

        if (request.AssignedUserId is { } assignee)
        {
            User user = await _users.GetByPublicIdAsync(assignee, cancellationToken)
                ?? throw NotFoundException.For(nameof(User), assignee, "public id");

            if (!user.IsActive)
            {
                throw new ForbiddenAccessException("A task cannot be assigned to an inactive user.");
            }

            task.AssignTo(assignee);
        }
        else
        {
            task.Unassign();
        }

        _tasks.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskSummary.FromTask(task);
    }
}
