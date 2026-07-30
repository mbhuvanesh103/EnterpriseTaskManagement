using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities;
using EnterpriseTaskManagement.Domain.Exceptions;
using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand, DeleteUserResponse>
{
    private readonly IUserRepository _userRepository;

    public DeleteUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<DeleteUserResponse> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByPublicIdAsync(
            request.PublicId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                nameof(User),
                request.PublicId);
        }

        user.Delete(request.DeletedBy);

        await _userRepository.SaveChangesAsync(
            cancellationToken);

        return new DeleteUserResponse(
            user.PublicId,
            "User deleted successfully.");
    }
}