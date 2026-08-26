using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Use-case input for registering a user. Primitives only: the handler turns
/// them into Domain value objects.
/// </summary>
public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string EmployeeCode,
    Guid DepartmentId) : IRequest<CreateUserResponse>;
