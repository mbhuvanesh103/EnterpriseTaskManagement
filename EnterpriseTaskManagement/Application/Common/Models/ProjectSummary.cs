using EnterpriseTaskManagement.Domain.Entities.Projects;

namespace EnterpriseTaskManagement.Application.Common.Models;

/// <summary>
/// Read model shared by the project queries. A DTO exposing primitives only,
/// mirroring <see cref="DepartmentSummary"/> and <see cref="UserSummary"/>.
/// </summary>
public sealed record ProjectSummary(
    Guid PublicId,
    string Name,
    string Code,
    string? Description,
    Guid DepartmentId,
    Guid ManagerId,
    string Status,
    DateTimeOffset StartDateUtc,
    DateTimeOffset? TargetEndDateUtc)
{
    public static ProjectSummary FromProject(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return new ProjectSummary(
            project.PublicId,
            project.Name,
            project.Code,
            project.Description,
            project.DepartmentId,
            project.ManagerId,
            project.Status.ToString(),
            project.StartDateUtc,
            project.TargetEndDateUtc);
    }
}
