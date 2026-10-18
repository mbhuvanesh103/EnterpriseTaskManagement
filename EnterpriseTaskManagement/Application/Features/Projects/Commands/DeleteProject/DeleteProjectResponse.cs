namespace EnterpriseTaskManagement.Application.Features.Projects.Commands.DeleteProject;

/// <summary>Result returned to the API after a project has been soft-deleted.</summary>
public sealed record DeleteProjectResponse(
    Guid PublicId,
    bool IsDeleted,
    DateTimeOffset? DeletedAtUtc);
