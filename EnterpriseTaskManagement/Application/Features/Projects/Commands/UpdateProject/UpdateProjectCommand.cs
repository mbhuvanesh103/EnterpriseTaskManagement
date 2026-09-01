using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.UpdateProject;

/// <summary>
/// Use-case input for updating a project, addressed by PublicId. The requester
/// identity comes from the authenticated token (never from the request body) so
/// the use case can enforce that a Manager may only change their own projects.
/// </summary>
public sealed record UpdateProjectCommand(
    Guid PublicId,
    string Name,
    string Code,
    Guid DepartmentId,
    Guid ManagerId,
    Guid RequesterPublicId,
    bool RequesterIsAdmin,
    string? Description = null,
    string? Status = null,
    DateTimeOffset? StartDateUtc = null,
    DateTimeOffset? TargetEndDateUtc = null) : IRequest<UpdateProjectResponse>;
