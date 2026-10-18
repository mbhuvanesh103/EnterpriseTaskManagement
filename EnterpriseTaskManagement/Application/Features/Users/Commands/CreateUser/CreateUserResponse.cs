namespace EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;

/// <summary>Result returned to the API after a user has been created.</summary>
public sealed record CreateUserResponse(
    Guid PublicId,
    string FullName,
    string Email,
    string EmployeeCode,
    Guid DepartmentId,
    bool IsActive);
