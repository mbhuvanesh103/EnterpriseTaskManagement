namespace EnterpriseTaskManagement.API.Contracts.Departments;

/// <summary>
/// Request body for <c>PUT /api/Departments/{publicId}</c>. It deliberately omits
/// <c>PublicId</c>: the identity of the resource can only come from the route, so
/// a mismatched or spoofed body identifier is impossible by construction.
/// </summary>
public sealed record UpdateDepartmentRequest(
    string Name,
    string Code,
    string? Description,
    bool IsActive = true);
