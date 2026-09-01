using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Projects;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.DeleteProject;

/// <summary>
/// Soft-deletes a project: the aggregate's own <c>Delete()</c> sets the audit
/// deletion state and cancels it. No row is ever physically removed. An Admin may
/// delete any project; a Manager only projects they lead.
/// </summary>
public sealed class DeleteProjectCommandHandler
    : IRequestHandler<DeleteProjectCommand, DeleteProjectResponse>
{
    private readonly IProjectRepository _projects;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProjectCommandHandler(IProjectRepository projects, IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteProjectResponse> Handle(
        DeleteProjectCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Project project = await _projects.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Project), request.PublicId, "public id");

        if (!request.RequesterIsAdmin && !project.IsManagedBy(request.RequesterPublicId))
        {
            throw new ForbiddenAccessException("You may only delete projects you manage.");
        }

        project.Delete();

        _projects.SoftDelete(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteProjectResponse(project.PublicId, project.IsDeleted, project.DeletedAtUtc);
    }
}
