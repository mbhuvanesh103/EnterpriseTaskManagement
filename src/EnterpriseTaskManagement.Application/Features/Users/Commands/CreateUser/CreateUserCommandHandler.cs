using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities;
using EnterpriseTaskManagement.Domain.ValueObjects;
using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<CreateUserResponse> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var name = FullName.Create(
            request.FirstName,
            request.LastName);

        var email = Email.Create(request.Email);

        var employeeCode = EmployeeCode.Create(request.EmployeeCode);

        var user = User.Create(
            name,
            email,
            employeeCode,
            request.DepartmentId);

        await _userRepository.AddAsync(user, cancellationToken);

        await _userRepository.SaveChangesAsync(cancellationToken);

        return new CreateUserResponse(user.PublicId, "User created successfully.");
    }
}