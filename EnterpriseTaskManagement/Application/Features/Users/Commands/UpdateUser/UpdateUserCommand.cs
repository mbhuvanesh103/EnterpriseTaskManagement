using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.UpdateUser;

/// <summary>Use-case input for updating an existing user, addressed by PublicId.</summary>
public sealed record UpdateUserCommand(
    Guid PublicId,
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId) : IRequest<UpdateUserResponse>;
