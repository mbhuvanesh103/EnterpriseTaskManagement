namespace EnterpriseTaskManagement.Domain.Entities.Users;

/// <summary>
/// The closed set of roles a user can hold. Strongly typed and framework-free:
/// the Domain is the single source of truth for role names, so no arbitrary
/// role strings can enter the system.
/// </summary>
public enum UserRole
{
    /// <summary>Default role. No user-management access.</summary>
    Employee = 0,

    /// <summary>Organizational read access to users.</summary>
    Manager = 1,

    /// <summary>Full user management.</summary>
    Admin = 2,
}
