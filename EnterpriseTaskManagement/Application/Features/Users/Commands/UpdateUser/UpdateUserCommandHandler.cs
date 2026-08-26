using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.Exceptions;
using EnterpriseTaskManagement.Domain.ValueObjects;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Loads the aggregate by PublicId, applies the changes through its behaviour
/// (ChangeName / ChangeEmail / TransferDepartment) and commits.
/// </summary>
public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        User user = await _users.GetByPublicIdAsync(request.PublicId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), request.PublicId, "public id");

        var name = FullName.Create(request.FirstName, request.LastName);
        var email = Email.Create(request.Email);

        if (email != user.Email && await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw new DomainException($"A user with email '{email.Value}' already exists.");
        }

        user.ChangeName(name);
        user.ChangeEmail(email);
        user.TransferDepartment(request.DepartmentId);

        _users.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateUserResponse(
            user.PublicId,
            user.Name.Value,
            user.Email.Value,
            user.EmployeeCode.Value,
            user.DepartmentId,
            user.IsActive);
    }
}
