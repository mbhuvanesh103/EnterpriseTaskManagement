namespace EnterpriseTaskManagement.API.Contracts.Tasks;

/// <summary>
/// Request body for <c>POST /api/Tasks</c>. Project and assignee are addressed by
/// their public ids; internal database ids are never accepted or returned.
/// </summary>
public sealed record CreateTaskRequest(
    string Title,
    Guid ProjectId,
    string? Description,
    Guid? AssignedUserId,
    string? Priority,
    DateTimeOffset? DueDateUtc);
