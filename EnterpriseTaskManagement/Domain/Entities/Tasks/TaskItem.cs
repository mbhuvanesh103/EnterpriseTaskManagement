using EnterpriseTaskManagement.Domain.Common;
using EnterpriseTaskManagement.Domain.Exceptions;

namespace EnterpriseTaskManagement.Domain.Entities.Tasks;

/// <summary>
/// A single unit of work belonging to a project and optionally assigned to a
/// user, and an aggregate root: every state change goes through the behaviour on
/// this type. It mirrors the shape of <c>Project</c>, <c>Department</c> and
/// <c>User</c> — internal <c>Id</c> for persistence, <c>PublicId</c> for the API,
/// auditing/soft-delete/<c>Version</c> inherited from
/// <see cref="AggregateRoot{TId}"/> — and stays framework-free.
/// </summary>
/// <remarks>
/// The type is called <c>TaskItem</c> rather than <c>Task</c> so it can never
/// collide with <c>System.Threading.Tasks.Task</c>; the API resource is still
/// <c>/api/Tasks</c>.
/// </remarks>
public sealed class TaskItem : AggregateRoot<long>
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

    private TaskItem(
        long id,
        Guid publicId,
        string title,
        string? description,
        Guid projectId,
        Guid? assignedUserId,
        TaskPriority priority,
        TaskItemStatus status,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset? completedAtUtc)
        : base(id)
    {
        PublicId = publicId;
        Title = title;
        Description = description;
        ProjectId = projectId;
        AssignedUserId = assignedUserId;
        Priority = priority;
        Status = status;
        DueDateUtc = dueDateUtc;
        CompletedAtUtc = completedAtUtc;
    }

    /// <summary>Stable, externally shareable identifier used by the API.</summary>
    public Guid PublicId { get; private set; }

    /// <summary>Trimmed, required title.</summary>
    public string Title { get; private set; }

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; private set; }

    /// <summary>Owning project, referenced by its <c>PublicId</c>.</summary>
    public Guid ProjectId { get; private set; }

    /// <summary>Assignee, referenced by the user's <c>PublicId</c>. Null when unassigned.</summary>
    public Guid? AssignedUserId { get; private set; }

    public TaskPriority Priority { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public DateTimeOffset? DueDateUtc { get; private set; }

    /// <summary>Set when the task reaches <see cref="TaskItemStatus.Completed"/>.</summary>
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    /// <summary>True while the task has not been completed.</summary>
    public bool IsOpen => Status != TaskItemStatus.Completed;

    /// <summary>True when the task currently has an assignee.</summary>
    public bool IsAssigned => AssignedUserId is not null;

    /// <summary>Factory for a brand new task. New tasks always start in Todo.</summary>
    public static TaskItem Create(
        string title,
        Guid projectId,
        string? description = null,
        Guid? assignedUserId = null,
        TaskPriority priority = TaskPriority.Medium,
        DateTimeOffset? dueDateUtc = null,
        Guid? publicId = null) =>
        new(
            default,
            publicId ?? Guid.NewGuid(),
            NormalizeTitle(title),
            NormalizeDescription(description),
            RequireId(projectId, "Task project"),
            NormalizeAssignee(assignedUserId),
            priority,
            TaskItemStatus.Todo,
            dueDateUtc,
            completedAtUtc: null);

    /// <summary>Rehydrates an existing task from persisted state.</summary>
    public static TaskItem Rehydrate(
        long id,
        Guid publicId,
        string title,
        string? description,
        Guid projectId,
        Guid? assignedUserId,
        TaskPriority priority,
        TaskItemStatus status,
        DateTimeOffset? dueDateUtc,
        DateTimeOffset? completedAtUtc) =>
        new(id, publicId, title, description, projectId, assignedUserId, priority, status, dueDateUtc, completedAtUtc);

    /// <summary>
    /// Applies the editable descriptive fields, re-validating every invariant.
    /// Deliberately excludes reassignment and status transitions: those are
    /// separate use cases with their own rules (<see cref="AssignTo"/>,
    /// <see cref="ChangeStatus"/>).
    /// </summary>
    public void Update(
        string title,
        string? description,
        TaskPriority priority,
        DateTimeOffset? dueDateUtc)
    {
        if (!IsOpen)
        {
            throw new DomainException("A completed task can no longer be edited.");
        }

        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Priority = priority;
        DueDateUtc = dueDateUtc;
    }

    /// <summary>Assigns the task to a user (referenced by the user's public id).</summary>
    public void AssignTo(Guid assignedUserId)
    {
        if (!IsOpen)
        {
            throw new DomainException("A completed task can no longer be reassigned.");
        }

        AssignedUserId = RequireId(assignedUserId, "Task assignee");
    }

    /// <summary>Removes the current assignee. Only an unstarted task may be unassigned.</summary>
    public void Unassign()
    {
        if (Status is not (TaskItemStatus.Todo or TaskItemStatus.Blocked))
        {
            throw new DomainException(
                $"A task in {Status} cannot be unassigned; move it back to Todo first.");
        }

        AssignedUserId = null;
    }

    /// <summary>
    /// Moves the task to another workflow state. <see cref="TaskItemStatus.Completed"/>
    /// is terminal, work can only progress once somebody is assigned, and only the
    /// transitions listed in <see cref="IsTransitionAllowed"/> are permitted.
    /// </summary>
    public void ChangeStatus(TaskItemStatus status)
    {
        if (Status == status)
        {
            return;
        }

        if (!IsOpen)
        {
            throw new DomainException("A completed task can no longer change status.");
        }

        if (!IsTransitionAllowed(Status, status))
        {
            throw new DomainException($"A task cannot move from {Status} to {status}.");
        }

        if (status is not (TaskItemStatus.Todo or TaskItemStatus.Blocked) && !IsAssigned)
        {
            throw new DomainException($"A task must be assigned before it can move to {status}.");
        }

        Status = status;
        CompletedAtUtc = status == TaskItemStatus.Completed ? DateTimeOffset.UtcNow : null;
    }

    /// <summary>
    /// Soft-deletes the task inside the domain: the audit deletion state is set.
    /// Rows are never physically removed.
    /// </summary>
    public void Delete(string? deletedBy = null)
    {
        if (IsDeleted)
        {
            return;
        }

        MarkDeleted(DateTimeOffset.UtcNow, deletedBy);
    }

    /// <summary>True when the supplied user public id is the current assignee.</summary>
    public bool IsAssignedTo(Guid userPublicId) =>
        AssignedUserId is { } assignee && assignee == userPublicId;

    /// <summary>The allowed workflow graph. Completed is terminal.</summary>
    private static bool IsTransitionAllowed(TaskItemStatus from, TaskItemStatus to) => from switch
    {
        TaskItemStatus.Todo => to is TaskItemStatus.InProgress or TaskItemStatus.Blocked,
        TaskItemStatus.InProgress => to is TaskItemStatus.Review or TaskItemStatus.Blocked or TaskItemStatus.Todo,
        TaskItemStatus.Review => to is TaskItemStatus.Completed or TaskItemStatus.InProgress or TaskItemStatus.Blocked,
        TaskItemStatus.Blocked => to is TaskItemStatus.Todo or TaskItemStatus.InProgress,
        _ => false,
    };

    private static Guid RequireId(Guid value, string label) =>
        value == Guid.Empty
            ? throw new DomainException($"{label} is required.")
            : value;

    private static Guid? NormalizeAssignee(Guid? assignedUserId) =>
        assignedUserId is null or { } when assignedUserId == Guid.Empty ? null : assignedUserId;

    private static string NormalizeTitle(string title)
    {
        var normalized = (title ?? string.Empty).Trim();

        if (normalized.Length == 0)
        {
            throw new DomainException("Task title is required.");
        }

        if (normalized.Length > MaxTitleLength)
        {
            throw new DomainException($"Task title must not exceed {MaxTitleLength} characters.");
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
            throw new DomainException($"Task description must not exceed {MaxDescriptionLength} characters.");
        }

        return normalized;
    }
}
