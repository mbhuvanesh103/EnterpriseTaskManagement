using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Users;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;

/// <summary>
/// Soft-deletes a user: the aggregate's own <c>Delete()</c> sets the audit
/// deletion state, deactivates the user and raises <c>UserDeletedDomainEvent</c>.
/// No row is ever physically removed.
/// </summary>
public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, DeleteUserResponse>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteUserResponse> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        User user = await _users.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.PublicId, "public id");

        user.Delete();

        _users.SoftDelete(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteUserResponse(user.PublicId, user.IsDeleted, user.DeletedAtUtc);
    }
}
