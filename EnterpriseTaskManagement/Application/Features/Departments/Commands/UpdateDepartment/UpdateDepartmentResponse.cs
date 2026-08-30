namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.UpdateDepartment;

/// <summary>Result returned to the API after a department has been updated.</summary>
public sealed record UpdateDepartmentResponse(
    Guid PublicId,
    string Name,
    string Code,
    string? Description,
    bool IsActive);
