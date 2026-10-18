namespace EnterpriseTaskManagement.Application.Features.Auth.Commands.Login;

/// <summary>
/// Successful login payload. Carries identity plus the user's role name — no
/// password hash, no database identity, no secret or configuration value.
/// </summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid UserPublicId,
    string FullName,
    string Email,
    string Role);
