using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;

namespace EnterpriseTaskManagement.Domain.Entities;

public sealed class Department : AuditableEntity
{
    public string Name { get; private set; }

    public string Code { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    private Department(
        string name,
        string code,
        string? description)
    {
        Name = name;
        Code = code;
        Description = description;
        IsActive = true;
    }

    public static Department Create(
        string name,
        string code,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Department name is required.");

        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("Department code is required.");

        name = name.Trim();
        code = code.Trim().ToUpperInvariant();

        if (name.Length > 100)
            throw new ValidationException("Department name cannot exceed 100 characters.");

        if (code.Length > 20)
            throw new ValidationException("Department code cannot exceed 20 characters.");

        if (description?.Length > 500)
            throw new ValidationException("Description cannot exceed 500 characters.");

        return new Department(name, code, description?.Trim());
    }
}