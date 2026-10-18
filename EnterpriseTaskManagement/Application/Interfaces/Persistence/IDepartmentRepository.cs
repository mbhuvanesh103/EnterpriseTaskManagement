using EnterpriseTaskManagement.Domain.Entities.Departments;

namespace EnterpriseTaskManagement.Application.Interfaces.Persistence;

/// <summary>
/// Persistence abstraction for the <see cref="Department"/> aggregate. Declared in
/// Application and implemented in Infrastructure, so handlers never see EF Core
/// types (no <c>IQueryable</c>, no <c>DbSet</c>, no provider types leak here).
/// </summary>
public interface IDepartmentRepository
{
    Task<Department?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>True when another, non-deleted department already uses this code.</summary>
    Task<bool> CodeExistsAsync(string code, Guid? excludePublicId = null, CancellationToken cancellationToken = default);

    /// <summary>All departments that are not soft-deleted, ordered by code.</summary>
    Task<IReadOnlyList<Department>> ListAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Department department, CancellationToken cancellationToken = default);

    void Update(Department department);

    /// <summary>Persists the aggregate's already-applied soft-delete state.</summary>
    void SoftDelete(Department department, string? deletedBy = null);
}
