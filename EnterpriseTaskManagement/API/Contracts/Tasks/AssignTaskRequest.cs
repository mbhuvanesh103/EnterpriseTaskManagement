namespace EnterpriseTaskManagement.API.Contracts.Tasks;

/// <summary>
/// Request body for <c>PUT /api/Tasks/{publicId}/assignee</c>. A null
/// <c>AssignedUserId</c> unassigns the task.
/// </summary>
public sealed record AssignTaskRequest(Guid? AssignedUserId);
