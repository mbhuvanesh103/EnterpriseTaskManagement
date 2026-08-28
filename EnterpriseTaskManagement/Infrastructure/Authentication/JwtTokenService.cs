using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using EnterpriseTaskManagement.Application.Interfaces.Authentication;
using EnterpriseTaskManagement.Domain.Entities.Users;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseTaskManagement.Infrastructure.Authentication;

/// <summary>
/// Issues HMAC-SHA256 signed JWT access tokens from <see cref="JwtSettings"/>.
/// Claims are limited to subject, public id, email and name — no roles and no
/// permissions (role-based authorization is a later phase).
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    /// <summary>Claim carrying the user's externally shareable identifier.</summary>
    public const string PublicIdClaimType = "publicId";

    private readonly JwtSettings _settings;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value;

        byte[] key = Encoding.UTF8.GetBytes(_settings.SecretKey);
        if (key.Length < JwtSettings.MinimumSecretKeyBytes)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey must be at least 32 bytes. Provide it via User Secrets or the " +
                "Jwt__SecretKey environment variable; it is never stored in the repository.");
        }

        _credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        DateTimeOffset issuedAt = DateTimeOffset.UtcNow;
        DateTimeOffset expiresAt = issuedAt.AddMinutes(_settings.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.PublicId.ToString("N")),
            new(PublicIdClaimType, user.PublicId.ToString("N")),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name, user.Name.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);

        string value = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessToken(value, expiresAt);
    }

    /// <summary>Registered claim names used by this service.</summary>
    private static class JwtRegisteredClaimNames
    {
        internal const string Sub = "sub";
        internal const string Email = "email";
        internal const string Name = "name";
        internal const string Jti = "jti";
    }
}
