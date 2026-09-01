using EnterpriseTaskManagement.Domain.Entities.Projects;

namespace EnterpriseTaskManagement.Application.Interfaces.Persistence;

/// <summary>
/// Persistence abstraction for the <see cref="Project"/> aggregate. Declared in
/// Application and implemented in Infrastructure, so handlers never see EF Core
/// types (no <c>IQueryable</c>, no <c>DbSet</c>, no provider types leak here).
/// </summary>
public interface IProjectRepository
{
    Task<Project?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    /// <summary>True when another, non-deleted project already uses this code.</summary>
    Task<bool> CodeExistsAsync(string code, Guid? excludePublicId = null, CancellationToken cancellationToken = default);

    /// <summary>All projects that are not soft-deleted, ordered by code.</summary>
    Task<IReadOnlyList<Project>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Non-deleted projects owned by one department.</summary>
    Task<IReadOnlyList<Project>> ListByDepartmentAsync(Guid departmentPublicId, CancellationToken cancellationToken = default);

    /// <summary>Non-deleted projects led by one manager (user public id).</summary>
    Task<IReadOnlyList<Project>> ListByManagerAsync(Guid managerPublicId, CancellationToken cancellationToken = default);

    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    void Update(Project project);

    /// <summary>Persists the aggregate's already-applied soft-delete state.</summary>
    void SoftDelete(Project project, string? deletedBy = null);
}
