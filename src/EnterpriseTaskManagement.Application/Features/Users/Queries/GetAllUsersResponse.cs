namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetAllUsers;

public sealed record GetAllUsersResponse(
    string PublicId,
    string FullName,
    string Email,
    string EmployeeCode,
    bool IsActive
);