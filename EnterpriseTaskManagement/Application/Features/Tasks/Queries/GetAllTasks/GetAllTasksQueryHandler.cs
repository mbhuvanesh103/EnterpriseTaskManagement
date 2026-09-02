using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Tasks;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetAllTasks;

/// <summary>
/// Query handler: repository read → DTO projection, with the role scope applied
/// before any optional filter, so a Manager or Employee can never read outside
/// their own scope by passing a filter. No DbContext.
/// </summary>
public sealed class GetAllTasksQueryHandler : IRequestHandler<GetAllTasksQuery, GetAllTasksResponse>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;

    public GetAllTasksQueryHandler(ITaskRepository tasks, IProjectRepository projects)
    {
        _tasks = tasks;
        _projects = projects;
    }

    public async Task<GetAllTasksResponse> Handle(GetAllTasksQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<TaskItem> scoped = request.RequesterRole switch
        {
            // An Employee only ever sees their own assigned tasks.
            TaskRequesterRole.Employee =>
                await _tasks.ListByAssigneeAsync(request.RequesterPublicId, cancellationToken),

            // A Manager sees every task inside the projects they lead.
            TaskRequesterRole.Manager =>
                await _tasks.ListByProjectManagerAsync(request.RequesterPublicId, cancellationToken),

            _ => await ListForAdminAsync(request, cancellationToken),
        };

        IEnumerable<TaskItem> filtered = scoped;

        if (request.RequesterRole != TaskRequesterRole.Admin)
        {
            if (request.ProjectId is { } project)
            {
                filtered = filtered.Where(task => task.ProjectId == project);
            }

            if (request.AssignedUserId is { } assignee)
            {
                filtered = filtered.Where(task => task.AssignedUserId == assignee);
            }
        }

        var summaries = filtered.Select(TaskSummary.FromTask).ToList();

        return new GetAllTasksResponse(summaries, summaries.Count);
    }

    private async Task<IReadOnlyList<TaskItem>> ListForAdminAsync(
        GetAllTasksQuery request,
        CancellationToken cancellationToken) => request switch
        {
            { ProjectId: { } project } => await _tasks.ListByProjectAsync(project, cancellationToken),
            { AssignedUserId: { } assignee } => await _tasks.ListByAssigneeAsync(assignee, cancellationToken),
            _ => await _tasks.ListAllAsync(cancellationToken),
        };

    // Referenced for documentation clarity: manager scope resolves through Project.
    private static bool LeadsProject(Project project, Guid managerPublicId) => project.IsManagedBy(managerPublicId);
}
