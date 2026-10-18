namespace EnterpriseTaskManagement.Application.Common.Interfaces;

/// <summary>
/// Commits a set of aggregate changes as one transaction. Implemented by the
/// Infrastructure DbContext; the Application layer only knows this contract.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
