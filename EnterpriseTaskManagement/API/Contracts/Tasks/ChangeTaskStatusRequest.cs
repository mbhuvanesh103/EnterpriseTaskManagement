namespace EnterpriseTaskManagement.API.Contracts.Tasks;

/// <summary>
/// Request body for <c>PUT /api/Tasks/{publicId}/status</c>. <c>Status</c> is one
/// of Todo, InProgress, Review, Completed, Blocked.
/// </summary>
public sealed record ChangeTaskStatusRequest(string Status);
