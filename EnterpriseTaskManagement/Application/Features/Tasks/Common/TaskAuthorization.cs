using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Tasks;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Common;

/// <summary>
/// The role a caller holds, as carried by the authenticated token. Passed into
/// task commands by the API so the Application layer can apply ownership rules
/// without ever touching <c>HttpContext</c>.
/// </summary>
public enum TaskRequesterRole
{
    Employee = 0,
    Manager = 1,
    Admin = 2,
}

/// <summary>
/// The single place the task ownership rules live, shared by every task use case:
/// <list type="bullet">
/// <item>Admin — full task management.</item>
/// <item>Manager — only tasks inside projects whose <c>ManagerId</c> equals the
/// authenticated user's public id, resolved from the task's project and never
/// from client input.</item>
/// <item>Employee — read-only on their own assigned tasks, plus a status change
/// on those tasks. Everything else is forbidden.</item>
/// </list>
/// </summary>
internal static class TaskAuthorization
{
    /// <summary>
    /// Resolves the project a task belongs to and asserts that the caller may
    /// manage that task (create/update/delete/assign). Employees are never
    /// allowed here.
    /// </summary>
    internal static async Task<Project> EnsureCanManageAsync(
        IProjectRepository projects,
        Guid projectPublicId,
        Guid requesterPublicId,
        TaskRequesterRole requesterRole,
        CancellationToken cancellationToken)
    {
        Project project = await projects.GetByPublicIdAsync(projectPublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Project), projectPublicId, "public id");

        switch (requesterRole)
        {
            case TaskRequesterRole.Admin:
                return project;

            case TaskRequesterRole.Manager when project.IsManagedBy(requesterPublicId):
                return project;

            default:
                throw new ForbiddenAccessException(
                    "You may only manage tasks inside projects you manage.");
        }
    }

    /// <summary>
    /// Asserts the caller may change the status of <paramref name="task"/>: Admin
    /// always, a Manager for tasks in projects they lead, and an Employee only for
    /// tasks assigned to themselves.
    /// </summary>
    internal static async Task EnsureCanChangeStatusAsync(
        IProjectRepository projects,
        TaskItem task,
        Guid requesterPublicId,
        TaskRequesterRole requesterRole,
        CancellationToken cancellationToken)
    {
        if (requesterRole == TaskRequesterRole.Employee)
        {
            if (!task.IsAssignedTo(requesterPublicId))
            {
                throw new ForbiddenAccessException(
                    "You may only change the status of tasks assigned to you.");
            }

            return;
        }

        _ = await EnsureCanManageAsync(
            projects,
            task.ProjectId,
            requesterPublicId,
            requesterRole,
            cancellationToken);
    }

    /// <summary>
    /// Asserts the caller may read <paramref name="task"/>: Admin always, a
    /// Manager for tasks in projects they lead, and an Employee only for tasks
    /// assigned to themselves.
    /// </summary>
    internal static async Task EnsureCanReadAsync(
        IProjectRepository projects,
        TaskItem task,
        Guid requesterPublicId,
        TaskRequesterRole requesterRole,
        CancellationToken cancellationToken)
    {
        if (requesterRole == TaskRequesterRole.Admin)
        {
            return;
        }

        if (requesterRole == TaskRequesterRole.Employee)
        {
            if (!task.IsAssignedTo(requesterPublicId))
            {
                throw new ForbiddenAccessException("You may only read tasks assigned to you.");
            }

            return;
        }

        Project project = await projects.GetByPublicIdAsync(task.ProjectId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Project), task.ProjectId, "public id");

        if (!project.IsManagedBy(requesterPublicId))
        {
            throw new ForbiddenAccessException("You may only read tasks inside projects you manage.");
        }
    }
}
