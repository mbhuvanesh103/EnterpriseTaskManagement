using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Exceptions;
using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetUserByPublicId;

public sealed class GetUserByPublicIdQueryHandler
    : IRequestHandler<GetUserByPublicIdQuery, GetUserByPublicIdResponse>
{
    private readonly IUserRepository _userRepository;

    public GetUserByPublicIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetUserByPublicIdResponse> Handle(
        GetUserByPublicIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPublicIdAsync(
            request.PublicId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User", request.PublicId);
        }

        return new GetUserByPublicIdResponse(
            user.PublicId,
            user.Name.ToString(),
            user.Email.Value,
            user.EmployeeCode.Value,
            user.IsActive
        );
    }
}