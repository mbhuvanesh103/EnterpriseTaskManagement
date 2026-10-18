using System.Text.RegularExpressions;

using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;

namespace EnterpriseTaskManagement.Domain.Entities.Projects;

/// <summary>
/// A unit of work owned by a department and led by a manager, and an aggregate
/// root: every state change goes through the behaviour on this type. It mirrors
/// the shape of <c>Department</c> and <c>User</c> — internal <c>Id</c> for
/// persistence, <c>PublicId</c> for the API, auditing/soft-delete/<c>Version</c>
/// inherited from <see cref="AggregateRoot{TId}"/> — and stays framework-free.
/// </summary>
public sealed class Project : AggregateRoot<long>
{
    public const int MaxNameLength = 200;
    public const int MaxCodeLength = 20;
    public const int MaxDescriptionLength = 1000;

    /// <summary>
    /// Domain-owned code format: 2-20 upper-case letters, digits or hyphens,
    /// starting with a letter (for example <c>APOLLO</c>, <c>ERP-2026</c>).
    /// </summary>
    private static readonly Regex CodePattern = new(
        "^[A-Z][A-Z0-9-]{1,19}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private Project(
        long id,
        Guid publicId,
        string name,
        string code,
        string? description,
        Guid departmentId,
        Guid managerId,
        ProjectStatus status,
        DateTimeOffset startDateUtc,
        DateTimeOffset? targetEndDateUtc)
        : base(id)
    {
        PublicId = publicId;
        Name = name;
        Code = code;
        Description = description;
        DepartmentId = departmentId;
        ManagerId = managerId;
        Status = status;
        StartDateUtc = startDateUtc;
        TargetEndDateUtc = targetEndDateUtc;
    }

    /// <summary>Stable, externally shareable identifier used by the API.</summary>
    public Guid PublicId { get; private set; }

    /// <summary>Trimmed, required display name.</summary>
    public string Name { get; private set; }

    /// <summary>Short project code, normalized to upper case and unique.</summary>
    public string Code { get; private set; }

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; private set; }

    /// <summary>Owning department, referenced by its <c>PublicId</c>.</summary>
    public Guid DepartmentId { get; private set; }

    /// <summary>Responsible manager, referenced by the user's <c>PublicId</c>.</summary>
    public Guid ManagerId { get; private set; }

    /// <summary>Current lifecycle state.</summary>
    public ProjectStatus Status { get; private set; }

    public DateTimeOffset StartDateUtc { get; private set; }

    public DateTimeOffset? TargetEndDateUtc { get; private set; }

    /// <summary>True while the project is neither completed nor cancelled.</summary>
    public bool IsOpen => Status is not (ProjectStatus.Completed or ProjectStatus.Cancelled);

    /// <summary>Factory for a brand new project.</summary>
    public static Project Create(
        string name,
        string code,
        Guid departmentId,
        Guid managerId,
        string? description = null,
        ProjectStatus status = ProjectStatus.Planned,
        DateTimeOffset? startDateUtc = null,
        DateTimeOffset? targetEndDateUtc = null,
        Guid? publicId = null)
    {
        var start = startDateUtc ?? DateTimeOffset.UtcNow;

        return new Project(
            default,
            publicId ?? Guid.NewGuid(),
            NormalizeName(name),
            NormalizeCode(code),
            NormalizeDescription(description),
            RequireId(departmentId, "Project department"),
            RequireId(managerId, "Project manager"),
            status,
            start,
            ValidateSchedule(start, targetEndDateUtc));
    }

    /// <summary>Rehydrates an existing project from persisted state.</summary>
    public static Project Rehydrate(
        long id,
        Guid publicId,
        string name,
        string code,
        string? description,
        Guid departmentId,
        Guid managerId,
        ProjectStatus status,
        DateTimeOffset startDateUtc,
        DateTimeOffset? targetEndDateUtc) =>
        new(id, publicId, name, code, description, departmentId, managerId, status, startDateUtc, targetEndDateUtc);

    /// <summary>Applies a full set of changes, re-validating every invariant.</summary>
    public void Update(
        string name,
        string code,
        string? description,
        Guid departmentId,
        Guid managerId,
        ProjectStatus status,
        DateTimeOffset startDateUtc,
        DateTimeOffset? targetEndDateUtc)
    {
        Name = NormalizeName(name);
        Code = NormalizeCode(code);
        Description = NormalizeDescription(description);
        DepartmentId = RequireId(departmentId, "Project department");
        ManagerId = RequireId(managerId, "Project manager");
        Status = status;
        StartDateUtc = startDateUtc;
        TargetEndDateUtc = ValidateSchedule(startDateUtc, targetEndDateUtc);
    }

    public void Rename(string name) => Name = NormalizeName(name);

    public void ChangeCode(string code) => Code = NormalizeCode(code);

    public void ChangeDescription(string? description) => Description = NormalizeDescription(description);

    /// <summary>Reassigns the project to another manager (a user's <c>PublicId</c>).</summary>
    public void ReassignManager(Guid managerId) => ManagerId = RequireId(managerId, "Project manager");

    /// <summary>Moves the project to another owning department.</summary>
    public void TransferDepartment(Guid departmentId) =>
        DepartmentId = RequireId(departmentId, "Project department");

    /// <summary>Moves the project to another lifecycle state. Terminal states are final.</summary>
    public void ChangeStatus(ProjectStatus status)
    {
        if (Status == status)
        {
            return;
        }

        if (!IsOpen)
        {
            throw new DomainException(
                $"Project '{Code}' is {Status} and its status can no longer change.");
        }

        Status = status;
    }

    /// <summary>
    /// Soft-deletes the project inside the domain: the audit deletion state is set
    /// and the project is cancelled. Rows are never physically removed.
    /// </summary>
    public void Delete(string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        MarkDeleted(DateTimeOffset.UtcNow, deletedBy);

        if (IsOpen)
        {
            Status = ProjectStatus.Cancelled;
        }
    }

    /// <summary>True when the supplied user public id leads this project.</summary>
    public bool IsManagedBy(Guid userPublicId) => ManagerId == userPublicId;

    private static Guid RequireId(Guid value, string label) =>
        value == Guid.Empty
            ? throw new DomainException($"{label} is required.")
            : value;

    private static DateTimeOffset? ValidateSchedule(DateTimeOffset startUtc, DateTimeOffset? targetEndUtc)
    {
        if (targetEndUtc is { } end && end < startUtc)
        {
            throw new DomainException("Project target end date must not precede its start date.");
        }

        return targetEndUtc;
    }

    private static string NormalizeName(string name)
    {
        var normalized = (name ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new DomainException("Project name is required.");
        }

        if (normalized.Length > MaxNameLength)
        {
            throw new DomainException($"Project name must not exceed {MaxNameLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (normalized.Length == 0)
        {
            throw new DomainException("Project code is required.");
        }

        if (normalized.Length > MaxCodeLength)
        {
            throw new DomainException($"Project code must not exceed {MaxCodeLength} characters.");
        }

        if (!CodePattern.IsMatch(normalized))
        {
            throw new DomainException(
                "Project code must start with a letter and contain only letters, digits or hyphens.");
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
            throw new DomainException($"Project description must not exceed {MaxDescriptionLength} characters.");
        }

        return normalized;
    }
}
