using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Features.Tasks.Common;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Tasks;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.DeleteTask;

/// <summary>
/// Loads the task, enforces the project-manager ownership rule, then soft-deletes
/// it through the aggregate. Rows are never physically removed.
/// </summary>
public sealed class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, DeleteTaskResponse>
{
    private readonly ITaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTaskCommandHandler(
        ITaskRepository tasks,
        IProjectRepository projects,
        IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteTaskResponse> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
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

        task.Delete();

        _tasks.SoftDelete(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteTaskResponse(task.PublicId, task.IsDeleted, task.DeletedAtUtc);
    }
}
