using EnterpriseTaskManagement.Application.Interfaces.Persistence;
using EnterpriseTaskManagement.Domain.Entities.Departments;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDepartmentRepository"/>. Queries go
/// through <see cref="ApplicationDbContext"/>; the soft-delete query filter in
/// <c>DepartmentConfiguration</c> keeps deleted rows out of every read.
/// Persisting is the caller's responsibility via the unit of work.
/// </summary>
public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DepartmentRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public Task<Department?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        _dbContext.Departments.FirstOrDefaultAsync(d => d.PublicId == publicId, cancellationToken);

    public Task<bool> CodeExistsAsync(
        string code,
        Guid? excludePublicId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.Departments.AnyAsync(
            d => d.Code == code && (excludePublicId == null || d.PublicId != excludePublicId),
            cancellationToken);

    public async Task<IReadOnlyList<Department>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _dbContext.Departments
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);

        return departments;
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(department);
        await _dbContext.Departments.AddAsync(department, cancellationToken);
    }

    public void Update(Department department)
    {
        ArgumentNullException.ThrowIfNull(department);
        _dbContext.Departments.Update(department);
    }

    public void SoftDelete(Department department, string? deletedBy = null)
    {
        ArgumentNullException.ThrowIfNull(department);

        // The aggregate normally soft-deletes itself; this keeps the operation
        // idempotent for other callers. Rows are never physically removed.
        if (!department.IsDeleted)
        {
            department.MarkDeleted(DateTimeOffset.UtcNow, deletedBy);
        }

        _dbContext.Departments.Update(department);
    }
}
