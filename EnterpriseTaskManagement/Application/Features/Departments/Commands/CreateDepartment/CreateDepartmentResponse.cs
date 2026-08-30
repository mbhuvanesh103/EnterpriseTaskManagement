namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.CreateDepartment;

/// <summary>Result returned to the API after a department has been created.</summary>
public sealed record CreateDepartmentResponse(
    Guid PublicId,
    string Name,
    string Code,
    string? Description,
    bool IsActive);
