using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Tasks;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ITaskRepository"/>. Queries go through
/// <see cref="ApplicationDbContext"/>; the soft-delete query filters configured in
/// <c>TaskItemConfiguration</c> and <c>ProjectConfiguration</c> keep deleted rows
/// out of every read automatically. Persisting is the caller's responsibility via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TaskRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public Task<TaskItem?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        _dbContext.Tasks.FirstOrDefaultAsync(t => t.PublicId == publicId, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _dbContext.Tasks
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return tasks;
    }

    public async Task<IReadOnlyList<TaskItem>> ListByProjectAsync(
        Guid projectPublicId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _dbContext.Tasks
            .Where(t => t.ProjectId == projectPublicId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return tasks;
    }

    public async Task<IReadOnlyList<TaskItem>> ListByAssigneeAsync(
        Guid assignedUserPublicId,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _dbContext.Tasks
            .Where(t => t.AssignedUserId == assignedUserPublicId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return tasks;
    }

    public async Task<IReadOnlyList<TaskItem>> ListByProjectManagerAsync(
        Guid managerPublicId,
        CancellationToken cancellationToken = default)
    {
        // Manager scope is resolved server-side through the Project the task
        // belongs to, never from anything the client sends.
        var tasks = await _dbContext.Tasks
            .Where(t => _dbContext.Projects
                .Where(p => p.ManagerId == managerPublicId)
                .Select(p => p.PublicId)
                .Contains(t.ProjectId))
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return tasks;
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        await _dbContext.Tasks.AddAsync(task, cancellationToken);
    }

    public void Update(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        _dbContext.Tasks.Update(task);
    }

    public void SoftDelete(TaskItem task, string? deletedBy = null)
    {
        ArgumentNullException.ThrowIfNull(task);

        // The aggregate normally soft-deletes itself; this keeps the operation
        // idempotent for other callers. Rows are never physically removed.
        if (!task.IsDeleted)
        {
            task.MarkDeleted(DateTimeOffset.UtcNow, deletedBy);
        }

        _dbContext.Tasks.Update(task);
    }
}
