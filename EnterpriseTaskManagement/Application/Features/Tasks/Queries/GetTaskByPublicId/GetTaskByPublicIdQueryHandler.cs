using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Models;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Tasks;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Queries.GetTaskByPublicId;

/// <summary>
/// Query handler: repository read → DTO projection, after the read scope check
/// (Admin anywhere, Manager inside projects they lead, Employee only their own
/// assigned task). No DbContext.
/// </summary>
public sealed class GetTaskByPublicIdQueryHandler : IRequestHandler<GetTaskByPublicIdQuery, TaskSummary>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;

    public GetTaskByPublicIdQueryHandler(ITaskRepository tasks, IProjectRepository projects)
    {
        _tasks = tasks;
        _projects = projects;
    }

    public async Task<TaskSummary> Handle(GetTaskByPublicIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        TaskItem task = await _tasks.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(TaskItem), request.PublicId, "public id");

        await TaskAuthorization.EnsureCanReadAsync(
            _projects,
            task,
            request.RequesterPublicId,
            request.RequesterRole,
            cancellationToken);

        return TaskSummary.FromTask(task);
    }
}
