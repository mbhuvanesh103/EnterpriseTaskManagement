namespace EnterpriseTaskManagement.Application.Features.Users.Commands.DeleteUser;

/// <summary>Result returned to the API after a user has been soft-deleted.</summary>
public sealed record DeleteUserResponse(
    Guid PublicId,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);
