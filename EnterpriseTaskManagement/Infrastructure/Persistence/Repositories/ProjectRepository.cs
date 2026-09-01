using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Projects;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IProjectRepository"/>. Queries go through
/// <see cref="ApplicationDbContext"/>; the soft-delete query filter configured in
/// <c>ProjectConfiguration</c> keeps deleted rows out of every read automatically.
/// Persisting is the caller's responsibility via <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProjectRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public Task<Project?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        _dbContext.Projects.FirstOrDefaultAsync(p => p.PublicId == publicId, cancellationToken);

    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludePublicId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();

        return _dbContext.Projects.AnyAsync(
            p => p.Code == normalized && (excludePublicId == null || p.PublicId != excludePublicId),
            cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.Projects
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return projects;
    }

    public async Task<IReadOnlyList<Project>> ListByDepartmentAsync(
        Guid departmentPublicId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.Projects
            .Where(p => p.DepartmentId == departmentPublicId)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return projects;
    }

    public async Task<IReadOnlyList<Project>> ListByManagerAsync(
        Guid managerPublicId,
        CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.Projects
            .Where(p => p.ManagerId == managerPublicId)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return projects;
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        await _dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public void Update(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        _dbContext.Projects.Update(project);
    }

    public void SoftDelete(Project project, string? deletedBy = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        // The aggregate normally soft-deletes itself; this keeps the operation
        // idempotent for other callers. Rows are never physically removed.
        if (!project.IsDeleted)
        {
            project.MarkDeleted(DateTimeOffset.UtcNow, deletedBy);
        }

        _dbContext.Projects.Update(project);
    }
}
