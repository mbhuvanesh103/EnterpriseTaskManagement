using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Users;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.UpdateProject;

/// <summary>
/// Loads the aggregate by PublicId, enforces the ownership rule (an Admin may
/// change any project; a Manager only projects whose <c>ManagerId</c> equals the
/// authenticated user's public id), applies the changes through the aggregate's
/// behaviour and commits. Concurrent writes are rejected by the rowversion token
/// configured in Infrastructure and surface as HTTP 409.
/// </summary>
public sealed class UpdateProjectCommandHandler
    : IRequestHandler<UpdateProjectCommand, UpdateProjectResponse>
{
    private readonly IProjectRepository _projects;
    private readonly IDepartmentRepository _departments;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCommandHandler(
        IProjectRepository projects,
        IDepartmentRepository departments,
        IUserRepository users,
        IUnitOfWork unitOfWork)
    {
        _projects = projects;
        _departments = departments;
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateProjectResponse> Handle(
        UpdateProjectCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Project project = await _projects.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Project), request.PublicId, "public id");

        if (!request.RequesterIsAdmin && !project.IsManagedBy(request.RequesterPublicId))
        {
            throw new ForbiddenAccessException("You may only modify projects you manage.");
        }

        _ = await _departments.GetByPublicIdAsync(request.DepartmentId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Department), request.DepartmentId, "public id");

        User manager = await _users.GetByPublicIdAsync(request.ManagerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.ManagerId, "public id");

        if (manager.Role is not (UserRole.Manager or UserRole.Admin))
        {
            throw new ForbiddenAccessException(
                "A project manager must be a user with the Manager or Admin role.");
        }

        // A Manager cannot hand their own project to somebody else; only an Admin can reassign.
        if (!request.RequesterIsAdmin && request.ManagerId != request.RequesterPublicId)
        {
            throw new ForbiddenAccessException("Only an Admin may reassign a project to another manager.");
        }

        ProjectStatus status = ParseStatus(request.Status, project.Status);

        project.Update(
            request.Name,
            request.Code,
            request.Description,
            request.DepartmentId,
            request.ManagerId,
            status,
            request.StartDateUtc ?? project.StartDateUtc,
            request.TargetEndDateUtc);

        if (await _projects.CodeExistsAsync(project.Code, project.PublicId, cancellationToken))
        {
            throw ConflictException.Duplicate(nameof(Project), "code", project.Code);
        }

        _projects.Update(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateProjectResponse(
            project.PublicId,
            project.Name,
            project.Code,
            project.Description,
            project.DepartmentId,
            project.ManagerId,
            project.Status.ToString(),
            project.StartDateUtc,
            project.TargetEndDateUtc);
    }

    private static ProjectStatus ParseStatus(string? status, ProjectStatus current) =>
        status is null
            ? current
            : Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsed)
                ? parsed
                : throw new ArgumentException(
                    "Project status must be one of: Planned, Active, OnHold, Completed, Cancelled.",
                    nameof(status));
}
