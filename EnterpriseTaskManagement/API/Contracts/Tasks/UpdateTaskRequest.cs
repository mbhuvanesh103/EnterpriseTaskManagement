namespace EnterpriseTaskManagement.API.Contracts.Tasks;

/// <summary>
/// Request body for <c>PUT /api/Tasks/{publicId}</c>. It deliberately omits
/// <c>PublicId</c> (route-only identity), the assignee and the status: those are
/// changed through <c>PUT .../assignee</c> and <c>PUT .../status</c>. The
/// requester identity always comes from the token and can never be spoofed.
/// </summary>
public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    string? Priority,
    DateTimeOffset? DueDateUtc);
