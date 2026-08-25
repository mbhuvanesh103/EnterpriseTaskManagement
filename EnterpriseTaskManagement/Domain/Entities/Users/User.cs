using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Events.Users;
using EnterpriseTaskManagement.Domain.Exceptions;
using EnterpriseTaskManagement.Domain.ValueObjects;

namespace EnterpriseTaskManagement.Domain.Entities.Users;

/// <summary>
/// A member of the organization and an aggregate root: all changes to a user
/// go through the behaviour on this type, which keeps its invariants valid.
/// Every user must belong to a department.
/// </summary>
public sealed class User : AggregateRoot<long>
{
    // Constructor used by the factories and by persistence rehydration. It stays
    // in the Domain (no EF attributes) so Infrastructure can map without leaking.
    private User(
        long id,
        Guid publicId,
        FullName name,
        Email email,
        EmployeeCode employeeCode,
        Guid departmentId,
        bool isActive)
        : base(id)
    {
        PublicId = publicId;
        Name = name;
        Email = email;
        EmployeeCode = employeeCode;
        DepartmentId = departmentId;
        IsActive = isActive;
    }

    /// <summary>Stable, externally shareable identifier (exposed by the API).</summary>
    public Guid PublicId { get; private set; }

    /// <summary>The user's validated name.</summary>
    public FullName Name { get; private set; }

    /// <summary>The user's normalized email address.</summary>
    public Email Email { get; private set; }

    /// <summary>Organization-issued employee code.</summary>
    public EmployeeCode EmployeeCode { get; private set; }

    /// <summary>Public identifier of the department the user belongs to (required).</summary>
    public Guid DepartmentId { get; private set; }

    /// <summary>Whether the user may authenticate and be assigned work.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Factory for a brand new user.</summary>
    public static User Register(
        FullName name,
        Email email,
        EmployeeCode employeeCode,
        Guid departmentId,
        Guid? publicId = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(employeeCode);
        EnsureDepartment(departmentId);

        var user = new User(
            id: default,
            publicId: publicId ?? Guid.NewGuid(),
            name: name,
            email: email,
            employeeCode: employeeCode,
            departmentId: departmentId,
            isActive: true);

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.PublicId, email.Value, DateTimeOffset.UtcNow));
        return user;
    }

    /// <summary>Rehydrates an existing user from persisted state.</summary>
    public static User Rehydrate(
        long id,
        Guid publicId,
        FullName name,
        Email email,
        EmployeeCode employeeCode,
        Guid departmentId,
        bool isActive) =>
        new(id, publicId, name, email, employeeCode, departmentId, isActive);

    public void Rename(FullName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    public void ChangeEmail(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);
        if (Email == email)
        {
            return;
        }

        var previous = Email;
        Email = email;
        RaiseDomainEvent(new UserEmailChangedDomainEvent(PublicId, previous.Value, email.Value, DateTimeOffset.UtcNow));
    }

    /// <summary>Moves the user to another department. A department is mandatory.</summary>
    public void TransferToDepartment(Guid departmentId)
    {
        EnsureDepartment(departmentId);
        DepartmentId = departmentId;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static void EnsureDepartment(Guid departmentId)
    {
        if (departmentId == Guid.Empty)
        {
            throw new DomainException("Department identifier is required.");
        }
    }
}
