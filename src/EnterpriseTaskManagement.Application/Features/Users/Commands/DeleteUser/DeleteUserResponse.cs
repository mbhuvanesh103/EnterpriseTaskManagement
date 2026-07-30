namespace EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;

public sealed record DeleteUserResponse(
    string PublicId,
    string Message
);