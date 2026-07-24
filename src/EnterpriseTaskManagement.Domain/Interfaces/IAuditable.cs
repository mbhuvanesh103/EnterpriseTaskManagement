namespace EnterpriseTaskManagement.Domain.Common.Interfaces;

public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }

    string CreatedBy { get; }

    DateTimeOffset? UpdatedAt { get; }

    string? UpdatedBy { get; }
}