using EnterpriseTaskManagement.Application.Common.Exceptions;
using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.ValueObjects;

using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Orchestrates user registration: value objects → aggregate → repository →
/// unit of work. No EF Core, no SQL, no DbContext here.
/// </summary>
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(IUserRepository users, IUnitOfWork unitOfWork)
    {
        _users = users;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Domain value objects validate and normalize the raw input.
        var name = FullName.Create(request.FirstName, request.LastName);
        var email = Email.Create(request.Email);
        var employeeCode = EmployeeCode.Create(request.EmployeeCode);

        // Cross-aggregate uniqueness is an application concern; the repository
        // abstraction exposes exactly these checks, so this is real behaviour.
        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            throw ConflictException.Duplicate(nameof(User), "email", email.Value);
        }

        if (await _users.EmployeeCodeExistsAsync(employeeCode, cancellationToken))
        {
            throw ConflictException.Duplicate(nameof(User), "employee code", employeeCode.Value);
        }

        var user = User.Create(name, email, employeeCode, request.DepartmentId);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(
            user.PublicId,
            user.Name.Value,
            user.Email.Value,
            user.EmployeeCode.Value,
            user.DepartmentId,
            user.IsActive);
    }
}
