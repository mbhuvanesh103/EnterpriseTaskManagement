using EnterpriseTaskManagement.Domain.Entities.Departments;

namespace EnterpriseTaskManagement.Application.Common.Models;

/// <summary>
/// Read model shared by the department queries. A DTO exposing primitives only,
/// mirroring <see cref="UserSummary"/>.
/// </summary>
public sealed record DepartmentSummary(
    Guid PublicId,
    string Name,
    string Code,
    string? Description,
    bool IsActive)
{
    public static DepartmentSummary FromDepartment(Department department)
    {
        ArgumentNullException.ThrowIfNull(department);
        return new DepartmentSummary(
            department.PublicId,
            department.Name,
            department.Code,
            department.Description,
            department.IsActive);
    }
}
