using MediatR;

namespace EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;

/// <summary>Use-case input for soft-deleting a user, addressed by PublicId.</summary>
public sealed record DeleteUserCommand(Guid PublicId) : IRequest<DeleteUserResponse>;
