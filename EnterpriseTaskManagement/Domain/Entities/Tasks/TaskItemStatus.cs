namespace EnterpriseTaskManagement.Domain.Entities.Tasks;

/// <summary>
/// The closed set of task workflow states. Named <c>TaskItemStatus</c> so it can
/// never be confused with <c>System.Threading.Tasks.TaskStatus</c>; the domain
/// vocabulary itself is Todo / InProgress / Review / Completed / Blocked.
/// </summary>
public enum TaskItemStatus
{
    Todo = 0,

    InProgress = 1,

    Review = 2,

    Completed = 3,

    Blocked = 4,
}
