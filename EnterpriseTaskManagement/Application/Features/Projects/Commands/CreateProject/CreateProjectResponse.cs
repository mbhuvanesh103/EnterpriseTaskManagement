namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.CreateProject;

/// <summary>Result returned to the API after a project has been created.</summary>
public sealed record CreateProjectResponse(
    Guid PublicId,
    string Name,
    string Code,
    string? Description,
    Guid DepartmentId,
    Guid ManagerId,
    string Status,
    DateTimeOffset StartDateUtc,
    DateTimeOffset? TargetEndDateUtc);
