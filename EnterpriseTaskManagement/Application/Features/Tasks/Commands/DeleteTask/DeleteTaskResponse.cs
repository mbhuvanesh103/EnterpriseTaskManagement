namespace EnterpriseTaskManagement.Application.Features.Tasks.Commands.DeleteTask;

/// <summary>Result returned to the API after a task has been soft-deleted.</summary>
public sealed record DeleteTaskResponse(
    Guid PublicId,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);
