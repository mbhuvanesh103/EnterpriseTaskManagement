namespace EnterpriseTaskManagement.Application.Features.Departments.Queries.GetDepartmentByPublicId;

/// <summary>Read model returned by <c>GetDepartmentByPublicIdQuery</c>.</summary>
public sealed record GetDepartmentByPublicIdResponse(
    Guid PublicId,
    string Name,
    string Code,
    string? Description,
    bool IsActive);
