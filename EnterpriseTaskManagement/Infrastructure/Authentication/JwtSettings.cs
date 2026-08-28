using System.ComponentModel.DataAnnotations;

namespace EnterpriseTaskManagement.Infrastructure.Authentication;

/// <summary>
/// Strongly typed JWT options bound from the <c>Jwt</c> configuration section.
/// Issuer, Audience and ExpirationMinutes are non-secret and may live in
/// appsettings; <see cref="SecretKey"/> must come from User Secrets, an
/// environment variable (<c>Jwt__SecretKey</c>) or a vault, and is never
/// committed to the repository.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Minimum key length in bytes required for HMAC-SHA256 signing.</summary>
    public const int MinimumSecretKeyBytes = 32;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    /// <summary>Symmetric signing key. Secret — supplied by configuration only.</summary>
    [Required]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Access-token lifetime in minutes.</summary>
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;
}
