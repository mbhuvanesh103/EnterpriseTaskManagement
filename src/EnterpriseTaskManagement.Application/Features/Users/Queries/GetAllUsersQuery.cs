using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetAllUsers;

public sealed record GetAllUsersQuery()
    : IRequest<List<GetAllUsersResponse>>;