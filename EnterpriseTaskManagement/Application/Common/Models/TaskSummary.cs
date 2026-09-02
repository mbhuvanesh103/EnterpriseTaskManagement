using EnterpriseTaskManagement.Domain.Entities.Tasks;

namespace EnterpriseTaskManagement.Application.Common.Models;

/// <summary>
/// Read model shared by every task use case. A DTO exposing primitives and
/// public ids only — internal database ids never leave the Application layer —
/// mirroring <see cref="ProjectSummary"/> and <see cref="UserSummary"/>.
/// </summary>
public sealed record TaskSummary(
    Guid PublicId,
    string Title,
    string? Description,
    Guid ProjectId,
    Guid? AssignedUserId,
    string Priority,
    string Status,
    DateTimeOffset? DueDateUtc,
    DateTimeOffset? CompletedAtUtc)
{
    public static TaskSummary FromTask(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return new TaskSummary(
            task.PublicId,
            task.Title,
            task.Description,
            task.ProjectId,
            task.AssignedUserId,
            task.Priority.ToString(),
            task.Status.ToString(),
            task.DueDateUtc,
            task.CompletedAtUtc);
    }
}
