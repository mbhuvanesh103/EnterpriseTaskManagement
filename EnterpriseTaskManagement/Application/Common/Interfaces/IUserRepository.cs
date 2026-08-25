using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.ValueObjects;

namespace EnterpriseTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction for the <see cref="User"/> aggregate. Declared in
/// Application and implemented in Infrastructure, so handlers never see EF Core
/// types (no <c>IQueryable</c>, no <c>DbSet</c>, no provider types leak here).
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<User?> GetByEmployeeCodeAsync(EmployeeCode employeeCode, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    Task<bool> EmployeeCodeExistsAsync(EmployeeCode employeeCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListByDepartmentAsync(Guid departmentPublicId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    void Update(User user);

    /// <summary>Soft-deletes the aggregate (sets the deletion audit state).</summary>
    void SoftDelete(User user, string? deletedBy = null);
}
