using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(
    string PublicId,
    string DeletedBy
) : IRequest<DeleteUserResponse>;