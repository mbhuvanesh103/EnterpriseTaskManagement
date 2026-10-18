using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.ValueObjects;

namespace EnterpriseTaskManagement.Application.Interfaces.Persistence;

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

    /// <summary>All users that are not soft-deleted, ordered by employee code.</summary>
    Task<IReadOnlyList<User>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListByDepartmentAsync(Guid departmentPublicId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    void Update(User user);

    /// <summary>Persists the aggregate's already-applied soft-delete state.</summary>
    void SoftDelete(User user, string? deletedBy = null);
}
