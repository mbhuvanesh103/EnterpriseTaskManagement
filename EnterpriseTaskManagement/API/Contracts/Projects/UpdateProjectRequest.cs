namespace EnterpriseTaskManagement.API.Contracts.Projects;

/// <summary>
/// Request body for <c>PUT /api/Projects/{publicId}</c>. It deliberately omits
/// <c>PublicId</c> (route-only identity) and the requester identity, which is
/// taken from the authenticated token and can never be spoofed by the body.
/// </summary>
public sealed record UpdateProjectRequest(
    string Name,
    string Code,
    Guid DepartmentId,
    Guid ManagerId,
    string? Description,
    string? Status,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? TargetEndDateUtc);
