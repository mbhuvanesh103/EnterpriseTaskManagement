using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    string PublicId,
    string FirstName,
    string LastName,
    string Email,
    int DepartmentId,
    bool IsActive
) : IRequest<UpdateUserResponse>;