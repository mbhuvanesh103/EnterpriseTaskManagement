using EnterpriseTaskManagement.Application.Common.Interfaces;
using EnterpriseTaskManagement.Domain.Entities.Users;
using EnterpriseTaskManagement.Domain.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace EnterpriseTaskManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>. Queries go through
/// <see cref="ApplicationDbContext"/>; the soft-delete query filter configured in
/// <c>UserConfiguration</c> keeps deleted rows out of every read automatically.
/// Persisting is the caller's responsibility via <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        _dbContext.Users.FirstOrDefaultAsync(u => u.PublicId == publicId, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<User?> GetByEmployeeCodeAsync(EmployeeCode employeeCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employeeCode);
        return _dbContext.Users.FirstOrDefaultAsync(u => u.EmployeeCode == employeeCode, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);
        return _dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public Task<bool> EmployeeCodeExistsAsync(EmployeeCode employeeCode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employeeCode);
        return _dbContext.Users.AnyAsync(u => u.EmployeeCode == employeeCode, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListByDepartmentAsync(
        Guid departmentPublicId,
        CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .Where(u => u.DepartmentId == departmentPublicId)
            .OrderBy(u => u.EmployeeCode)
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _dbContext.Users.Update(user);
    }

    public void SoftDelete(User user, string? deletedBy = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        user.MarkDeleted(DateTimeOffset.UtcNow, deletedBy);
        _dbContext.Users.Update(user);
    }
}
