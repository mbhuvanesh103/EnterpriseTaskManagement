using EnterpriseTaskManagement.Domain.Entities.Users;

namespace EnterpriseTaskManagement.Application.Common.Models;

/// <summary>
/// Read model shared by the user queries. A DTO: it exposes primitives only, so
/// Domain value objects never cross the API boundary.
/// </summary>
public sealed record UserSummary(
    Guid PublicId,
    string FullName,
    string Email,
    string EmployeeCode,
    bool IsActive)
{
    /// <summary>Projects a <see cref="User"/> aggregate onto the read model.</summary>
    public static UserSummary FromUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserSummary(
            user.PublicId,
            user.Name.Value,
            user.Email.Value,
            user.EmployeeCode.Value,
            user.IsActive);
    }
}
