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
        bool isActive,
        string? passwordHash = null)
        : base(id)
    {
        PublicId = publicId;
        Name = name;
        Email = email;
        EmployeeCode = employeeCode;
        DepartmentId = departmentId;
        IsActive = isActive;
        PasswordHash = passwordHash;
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

    /// <summary>
    /// Already-hashed credential, or <c>null</c> for an account provisioned
    /// without one (such an account can never authenticate). The Domain stores
    /// the value opaquely: it never hashes, verifies or accepts a plaintext
    /// password, and the value is never exposed by the API.
    /// </summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Whether the account has a credential and may attempt to log in.</summary>
    public bool HasPassword => !string.IsNullOrWhiteSpace(PasswordHash);

    /// <summary>
    /// Sets or replaces the stored credential during account provisioning. The
    /// caller must pass a value produced by the application's password hasher;
    /// plaintext must never reach this method.
    /// </summary>
    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
    }


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

    /// <summary>
    /// Use-case friendly alias of <see cref="Register"/> used by the Application
    /// layer's CreateUser handler.
    /// </summary>
    public static User Create(
        FullName name,
        Email email,
        EmployeeCode employeeCode,
        Guid departmentId,
        Guid? publicId = null) =>
        Register(name, email, employeeCode, departmentId, publicId);

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

    /// <summary>Alias of <see cref="Rename"/> expressing the use-case wording.</summary>
    public void ChangeName(FullName name) => Rename(name);

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

    /// <summary>Alias of <see cref="TransferToDepartment"/>.</summary>
    public void TransferDepartment(Guid departmentId) => TransferToDepartment(departmentId);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Soft-deletes the user inside the domain: the audit deletion state is set,
    /// the user is deactivated and a <see cref="UserDeletedDomainEvent"/> is raised.
    /// Physical rows are never removed.
    /// </summary>
    public void Delete(string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        MarkDeleted(DateTimeOffset.UtcNow, deletedBy);
        IsActive = false;
        RaiseDomainEvent(new UserDeletedDomainEvent(PublicId, Email.Value, DateTimeOffset.UtcNow));
    }

    private static void EnsureDepartment(Guid departmentId)
    {
        if (departmentId == Guid.Empty)
        {
            throw new DomainException("Department identifier is required.");
        }
    }
}
