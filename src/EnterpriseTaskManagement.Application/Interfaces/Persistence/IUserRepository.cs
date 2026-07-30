using EnterpriseTaskManagement.Domain.Entities;

namespace EnterpriseTaskManagement.Application.Interfaces.Persistence;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken);

    Task<List<User>> GetAllAsync(CancellationToken cancellationToken);

    Task<User?> GetByPublicIdAsync(
        string publicId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}