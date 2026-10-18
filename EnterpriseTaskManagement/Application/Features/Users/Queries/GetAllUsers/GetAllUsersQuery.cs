using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetAllUsers;

/// <summary>
/// Reads every user that is not soft-deleted. Optionally narrowed to a single
/// department (the repository abstraction supports that lookup).
/// </summary>
public sealed record GetAllUsersQuery(Guid? DepartmentId = null) : IRequest<GetAllUsersResponse>;
