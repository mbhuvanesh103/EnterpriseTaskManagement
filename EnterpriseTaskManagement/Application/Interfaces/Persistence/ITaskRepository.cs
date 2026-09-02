using EnterpriseTaskManagement.Domain.Entities.Tasks;

namespace EnterpriseTaskManagement.Application.Interfaces.Persistence;

/// <summary>
/// Persistence abstraction for the <see cref="TaskItem"/> aggregate. Declared in
/// Application and implemented in Infrastructure, so handlers never see EF Core
/// types (no <c>IQueryable</c>, no <c>DbSet</c>, no provider types leak here).
/// </summary>
public interface ITaskRepository
{
    Task<TaskItem?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>All tasks that are not soft-deleted, newest first.</summary>
    Task<IReadOnlyList<TaskItem>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Non-deleted tasks belonging to one project (project public id).</summary>
    Task<IReadOnlyList<TaskItem>> ListByProjectAsync(Guid projectPublicId, CancellationToken cancellationToken = default);

    /// <summary>Non-deleted tasks assigned to one user (user public id).</summary>
    Task<IReadOnlyList<TaskItem>> ListByAssigneeAsync(Guid assignedUserPublicId, CancellationToken cancellationToken = default);

    /// <summary>Non-deleted tasks across every project led by one manager (user public id).</summary>
    Task<IReadOnlyList<TaskItem>> ListByProjectManagerAsync(Guid managerPublicId, CancellationToken cancellationToken = default);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);

    void Update(TaskItem task);

    /// <summary>Persists the aggregate's already-applied soft-delete state.</summary>
    void SoftDelete(TaskItem task, string? deletedBy = null);
}
