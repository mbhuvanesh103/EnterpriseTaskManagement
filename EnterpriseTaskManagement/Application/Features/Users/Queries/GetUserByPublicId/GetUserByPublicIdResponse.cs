namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetUserByPublicId;

/// <summary>Read model returned by <c>GetUserByPublicIdQuery</c>.</summary>
public sealed record GetUserByPublicIdResponse(
    Guid PublicId,
    string FullName,
    string Email,
    string EmployeeCode,
    bool IsActive);
