using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Users;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Queries.GetUserByPublicId;

/// <summary>Reads one user, or throws <see cref="NotFoundException"/> when absent.</summary>
public sealed class GetUserByPublicIdQueryHandler
    : IRequestHandler<GetUserByPublicIdQuery, GetUserByPublicIdResponse>
{
    private readonly IUserRepository _users;

    public GetUserByPublicIdQueryHandler(IUserRepository users) => _users = users;

    public async Task<GetUserByPublicIdResponse> Handle(
        GetUserByPublicIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        User user = await _users.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.PublicId, "public id");

        return new GetUserByPublicIdResponse(
            user.PublicId,
            user.Name.Value,
            user.Email.Value,
            user.EmployeeCode.Value,
            user.IsActive);
    }
}
