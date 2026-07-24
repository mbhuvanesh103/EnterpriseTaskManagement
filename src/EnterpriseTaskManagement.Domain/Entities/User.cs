using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;
using EnterpriseTaskManagement.Domain.ValueObjects;
using EnterpriseTaskManagement.Domain.Events.User;

namespace EnterpriseTaskManagement.Domain.Entities;

public sealed class User : AuditableEntity
{
    public FullName Name { get; private set; } = null!;

    public Email Email { get; private set; } = null!;

    public EmployeeCode EmployeeCode { get; private set; } = null!;

    public int DepartmentId { get; private set; }

    public bool IsActive { get; private set; }

    // Required by EF Core
    private User()
    {
    }

    private User(
        FullName name,
        Email email,
        EmployeeCode employeeCode,
        int departmentId)
    {
        Name = name;
        Email = email;
        EmployeeCode = employeeCode;
        DepartmentId = departmentId;
        IsActive = true;
    }

    public static User Create(
        FullName name,
        Email email,
        EmployeeCode employeeCode,
        int departmentId)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(employeeCode);

        if (departmentId <= 0)
            throw new ValidationException("Department is required.");

        var user = new User(name, email, employeeCode, departmentId);

        user.AddDomainEvent(new UserCreatedEvent(user.PublicId));

        return user;
    }

    public void ChangeName(FullName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    public void ChangeEmail(Email email)
    {
        ArgumentNullException.ThrowIfNull(email);

        Email = email;
    }

    public void TransferDepartment(int departmentId)
    {
        if (departmentId <= 0)
            throw new ValidationException("Department is required.");

        DepartmentId = departmentId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}