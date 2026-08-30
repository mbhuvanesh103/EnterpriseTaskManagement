using System.Text.RegularExpressions;

using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;

namespace EnterpriseTaskManagement.Domain.Entities.Departments;

/// <summary>
/// Organizational unit a <see cref="Entities.Users.User"/> belongs to, and an
/// aggregate root: every change goes through the behaviour on this type. It
/// follows the same shape as <c>User</c> — internal <c>Id</c> for persistence,
/// <c>PublicId</c> for the API, auditing/soft-delete/<c>Version</c> inherited
/// from <see cref="AggregateRoot{TId}"/> — and stays framework-free.
/// </summary>
public sealed class Department : AggregateRoot<long>
{
    public const int MaxNameLength = 150;
    public const int MaxCodeLength = 20;
    public const int MaxDescriptionLength = 500;

    /// <summary>
    /// Domain-owned code format: 2-20 upper-case letters, digits or hyphens,
    /// starting with a letter (for example <c>ENG</c>, <c>HR-OPS</c>).
    /// </summary>
    private static readonly Regex CodePattern = new(
        "^[A-Z][A-Z0-9-]{1,19}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private Department(
        long id,
        Guid publicId,
        string name,
        string code,
        string? description,
        bool isActive)
        : base(id)
    {
        PublicId = publicId;
        Name = name;
        Code = code;
        Description = description;
        IsActive = isActive;
    }

    /// <summary>Stable, externally shareable identifier; users reference this value.</summary>
    public Guid PublicId { get; private set; }

    /// <summary>Trimmed, required display name.</summary>
    public string Name { get; private set; }

    /// <summary>Short organizational code, normalized to upper case and unique.</summary>
    public string Code { get; private set; }

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; private set; }

    /// <summary>Whether the department may receive new members.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Factory for a brand new department.</summary>
    public static Department Create(
        string name,
        string code,
        string? description = null,
        bool isActive = true,
        Guid? publicId = null) =>
        new(
            default,
            publicId ?? Guid.NewGuid(),
            NormalizeName(name),
            NormalizeCode(code),
            NormalizeDescription(description),
            isActive);

    /// <summary>Rehydrates an existing department from persisted state.</summary>
    public static Department Rehydrate(
        long id,
        Guid publicId,
        string name,
        string code,
        string? description,
        bool isActive) =>
        new(id, publicId, name, code, description, isActive);

    /// <summary>Applies a full set of changes, re-validating every invariant.</summary>
    public void Update(string name, string code, string? description, bool isActive)
    {
        Name = NormalizeName(name);
        Code = NormalizeCode(code);
        Description = NormalizeDescription(description);
        IsActive = isActive;
    }

    public void Rename(string name) => Name = NormalizeName(name);

    public void ChangeCode(string code) => Code = NormalizeCode(code);

    public void ChangeDescription(string? description) => Description = NormalizeDescription(description);

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Soft-deletes the department inside the domain: the audit deletion state is
    /// set and the department is deactivated. Rows are never physically removed.
    /// </summary>
    public void Delete(string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        MarkDeleted(DateTimeOffset.UtcNow, deletedBy);
        IsActive = false;
    }

    private static string NormalizeName(string name)
    {
        var normalized = (name ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new DomainException("Department name is required.");
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new DomainException($"Department name must not exceed {MaxNameLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (normalized.Length == 0)
        {
            throw new DomainException("Department code is required.");
        }

        if (normalized.Length > MaxCodeLength)
        {
            throw new DomainException($"Department code must not exceed {MaxCodeLength} characters.");
        }

        if (!CodePattern.IsMatch(normalized))
        {
            throw new DomainException(
                "Department code must start with a letter and contain only letters, digits or hyphens.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalized = description.Trim();

        if (normalized.Length > MaxDescriptionLength)
        {
            throw new DomainException($"Department description must not exceed {MaxDescriptionLength} characters.");
        }

        return normalized;
    }
}
