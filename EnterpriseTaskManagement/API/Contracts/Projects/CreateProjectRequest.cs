namespace EnterpriseTaskManagement.API.Contracts.Projects;

/// <summary>Request body for <c>POST /api/Projects</c>.</summary>
public sealed record CreateProjectRequest(
    string Name,
    string Code,
    Guid DepartmentId,
    Guid ManagerId,
    string? Description,
    string? Status,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? TargetEndDateUtc);
