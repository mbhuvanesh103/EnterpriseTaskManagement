namespace EnterpriseTaskManagement.Application.Interfaces.Authentication;

/// <summary>
/// Password hashing abstraction. Declared in Application so use cases never see
/// a cryptographic implementation; the real algorithm lives in Infrastructure.
/// Plaintext passwords are never stored, logged or returned anywhere.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a self-describing hash string safe to persist.</summary>
    string Hash(string password);

    /// <summary>Constant-time verification of a candidate password against a stored hash.</summary>
    bool Verify(string password, string passwordHash);
}
