using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;

namespace EnterpriseTaskManagement.Domain.Entities.Departments;

/// <summary>
/// Organizational unit a <see cref="Entities.Users.User"/> belongs to. Created
/// only to satisfy the required <c>User.DepartmentId</c> relationship; it stays
/// framework-free like the rest of the Domain.
/// </summary>
public sealed class Department : AggregateRoot<long>
{
    public const int MaxNameLength = 150;
    public const int MaxCodeLength = 20;

    private Department(long id, Guid publicId, string name, string code, bool isActive)
        : base(id)
    {
        PublicId = publicId;
        Name = name;
        Code = code;
        IsActive = isActive;
    }

    /// <summary>Stable, externally shareable identifier; users reference this value.</summary>
    public Guid PublicId { get; private set; }

    public string Name { get; private set; }

    /// <summary>Short organizational code, normalized to upper case.</summary>
    public string Code { get; private set; }

    public bool IsActive { get; private set; }

    public static Department Create(string name, string code, Guid? publicId = null)
    {
        var normalizedName = (name ?? string.Empty).Trim();
        var normalizedCode = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (normalizedName.Length == 0)
        {
            throw new DomainException("Department name is required.");
        }

        if (normalizedName.Length > MaxNameLength)
        {
            throw new DomainException($"Department name must not exceed {MaxNameLength} characters.");
        }

        if (normalizedCode.Length == 0)
        {
            throw new DomainException("Department code is required.");
        }

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new DomainException($"Department code must not exceed {MaxCodeLength} characters.");
        }

        return new Department(default, publicId ?? Guid.NewGuid(), normalizedName, normalizedCode, true);
    }

    public static Department Rehydrate(long id, Guid publicId, string name, string code, bool isActive) =>
        new(id, publicId, name, code, isActive);

    public void Rename(string name)
    {
        var renamed = Create(name, Code, PublicId);
        Name = renamed.Name;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
