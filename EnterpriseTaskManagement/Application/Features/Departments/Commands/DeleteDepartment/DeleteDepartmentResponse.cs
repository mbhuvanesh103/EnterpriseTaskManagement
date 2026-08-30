namespace EnterpriseTaskManagement.Application.Features.Departments.Commands.DeleteDepartment;

/// <summary>Result returned to the API after a department has been soft-deleted.</summary>
public sealed record DeleteDepartmentResponse(
    Guid PublicId,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);
