namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.UpdateProject;

/// <summary>Result returned to the API after a project has been updated.</summary>
public sealed record UpdateProjectResponse(
    Guid PublicId,
    string Name,
    string Code,
    string? Description,
    Guid DepartmentId,
    Guid ManagerId,
    string Status,
    DateTimeOffset StartDateUtc,
    DateTimeOffset? TargetEndDateUtc);
