namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetUserByPublicId;

public sealed record GetUserByPublicIdResponse(
    string PublicId,
    string FullName,
    string Email,
    string EmployeeCode,
    bool IsActive
);