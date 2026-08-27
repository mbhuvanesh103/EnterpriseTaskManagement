namespace EnterpriseTaskManagement.API.Contracts.Users;

/// <summary>
/// Request body for <c>PUT /api/Users/{publicId}</c>. It deliberately omits
/// <c>PublicId</c>: the identity of the resource can only come from the route,
/// so a mismatched or spoofed body identifier is impossible by construction.
/// </summary>
public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    Guid DepartmentId);
