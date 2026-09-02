namespace EnterpriseTaskManagement.Domain.Entities.Tasks;

/// <summary>
/// The closed set of task priorities. Strongly typed and framework-free, exactly
/// like <c>UserRole</c> and <c>ProjectStatus</c>, so no arbitrary priority
/// strings can ever enter the system.
/// </summary>
public enum TaskPriority
{
    Low = 0,

    Medium = 1,

    High = 2,

    Critical = 3,
}
