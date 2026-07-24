using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetAllUsers;

public sealed class GetAllUsersQueryHandler
    : IRequestHandler<GetAllUsersQuery, List<GetAllUsersResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetAllUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<GetAllUsersResponse>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);

        return users.Select(user => new GetAllUsersResponse(
            user.PublicId,
            user.Name.ToString(),
            user.Email.Value,
            user.EmployeeCode.Value,
            user.IsActive
        )).ToList();
    }
}