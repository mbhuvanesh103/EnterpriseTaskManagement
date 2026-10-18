namespace EnterpriseTaskManagement.Application.Features.Projects.Queries.GetProjectByPublicId;

/// <summary>Read model returned by <c>GetProjectByPublicIdQuery</c>.</summary>
public sealed record GetProjectByPublicIdResponse(
    Guid PublicId,
    string Name,
    string Code,
    string? Description,
    Guid DepartmentId,
    Guid ManagerId,
    string Status,
    DateTimeOffset StartDateUtc,
    DateTimeOffset? TargetEndDateUtc);
