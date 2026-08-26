using EnterpriseTaskManagement.Application.Common.Models;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetAllUsers;

/// <summary>Read model returned by <c>GetAllUsersQuery</c>.</summary>
public sealed record GetAllUsersResponse(IReadOnlyList<UserSummary> Users, int Count);
