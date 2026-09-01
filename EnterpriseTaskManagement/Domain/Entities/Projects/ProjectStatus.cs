namespace EnterpriseTaskManagement.Domain.Entities.Projects;

/// <summary>
/// The closed set of lifecycle states a <see cref="Project"/> can be in.
/// Strongly typed and framework-free, exactly like <c>UserRole</c>: the Domain is
/// the single source of truth, so no arbitrary status strings can enter the system.
/// </summary>
public enum ProjectStatus
{
    /// <summary>Default state. Approved but not started.</summary>
    Planned = 0,

    /// <summary>Work is in progress.</summary>
    Active = 1,

    /// <summary>Temporarily paused; may resume later.</summary>
    OnHold = 2,

    /// <summary>Finished successfully. Terminal.</summary>
    Completed = 3,

    /// <summary>Abandoned before completion. Terminal.</summary>
    Cancelled = 4,
}
