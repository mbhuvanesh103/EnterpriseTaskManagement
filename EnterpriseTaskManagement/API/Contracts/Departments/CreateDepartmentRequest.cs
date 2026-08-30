namespace EnterpriseTaskManagement.API.Contracts.Departments;

/// <summary>Request body for <c>POST /api/Departments</c>.</summary>
public sealed record CreateDepartmentRequest(
    string Name,
    string Code,
    string? Description,
    bool IsActive = true);
