using EnterpriseTaskManagement.Domain.Entities.Users;

namespace EnterpriseTaskManagement.Application.Interfaces.Authentication;

/// <summary>
/// Access-token issuing abstraction. The Application layer knows only this
/// interface — no <c>JwtSecurityToken</c>, no JwtBearer, no IConfiguration.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Issues a signed access token for an authenticated user.</summary>
    AccessToken CreateAccessToken(User user);
}

/// <summary>A signed access token and the instant it stops being valid.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);
