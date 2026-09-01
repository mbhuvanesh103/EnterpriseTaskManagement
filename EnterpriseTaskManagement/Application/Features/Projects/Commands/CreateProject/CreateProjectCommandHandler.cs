using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;
using EnterpriseTaskManagement.Domain.Entities.Projects;
using EnterpriseTaskManagement.Domain.Entities.Users;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// Validates the referenced department and manager exist, rejects duplicate
/// codes, then creates the aggregate through its own factory and commits.
/// </summary>
public sealed class CreateProjectCommandHandler
    : IRequestHandler<CreateProjectCommand, CreateProjectResponse>
{
    private readonly IProjectRepository _projects;
    private readonly IDepartmentRepository _departments;
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(
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

    public async Task<CreateProjectResponse> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _ = await _departments.GetByPublicIdAsync(request.DepartmentId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Department), request.DepartmentId, "public id");

        User manager = await _users.GetByPublicIdAsync(request.ManagerId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.ManagerId, "public id");

        if (manager.Role is not (UserRole.Manager or UserRole.Admin))
        {
            throw new ForbiddenAccessException(
                "A project manager must be a user with the Manager or Admin role.");
        }

        ProjectStatus status = ParseStatus(request.Status);

        Project project = Project.Create(
            request.Name,
            request.Code,
            request.DepartmentId,
            request.ManagerId,
            request.Description,
            status,
            request.StartDateUtc,
            request.TargetEndDateUtc);

        if (await _projects.CodeExistsAsync(project.Code, cancellationToken: cancellationToken))
        {
            throw ConflictException.Duplicate(nameof(Project), "code", project.Code);
        }

        await _projects.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProjectResponse(
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

    private static ProjectStatus ParseStatus(string? status) =>
        status is null
            ? ProjectStatus.Planned
            : Enum.TryParse<ProjectStatus>(status, ignoreCase: true, out var parsed)
                ? parsed
                : throw new ArgumentException(
                    "Project status must be one of: Planned, Active, OnHold, Completed, Cancelled.",
                    nameof(status));
}
