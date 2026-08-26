namespace EnterpriseTaskManagement.Application.Features.Users.Commands.UpdateUser;

/// <summary>Result returned to the API after a user has been updated.</summary>
public sealed record UpdateUserResponse(
    Guid PublicId,
    string FullName,
    string Email,
    string EmployeeCode,
    Guid DepartmentId,
    bool IsActive);
