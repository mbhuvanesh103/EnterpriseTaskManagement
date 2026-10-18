using System.Globalization;
using System.Security.Cryptography;

using EnterpriseTaskManagement.Application.Interfaces.Authentication;

namespace EnterpriseTaskManagement.Infrastructure.Authentication;

/// <summary>
/// PBKDF2 (RFC 2898) password hashing with HMAC-SHA256, a per-password random
/// salt and a high iteration count — the algorithm ASP.NET Core Identity uses,
/// implemented on .NET's own <see cref="Rfc2898DeriveBytes"/> so no third-party
/// crypto is introduced. Verification is constant time.
///
/// Stored format (all Base64, '.' separated):
///   <c>PBKDF2-SHA256.{iterations}.{salt}.{subkey}</c>
/// The iteration count travels with the hash, so it can be raised later without
/// invalidating existing credentials.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2-SHA256";
    private const int SaltSize = 16;
    private const int SubkeySize = 32;
    private const int DefaultIterations = 210_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] subkey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            SubkeySize);

        return string.Join('.',
            Prefix,
            DefaultIterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(subkey));
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        string[] parts = passwordHash.Split('.');
        if (parts.Length != 4 || !string.Equals(parts[0], Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int iterations)
            || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedSubkey;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedSubkey = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (salt.Length == 0 || expectedSubkey.Length == 0)
        {
            return false;
        }

        byte[] actualSubkey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedSubkey.Length);

        return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
    }
}
