using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string EmployeeCode,
    int DepartmentId
) : IRequest<CreateUserResponse>;