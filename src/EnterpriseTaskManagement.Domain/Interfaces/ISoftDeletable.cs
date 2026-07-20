namespace EnterpriseTaskManagement.Domain.Common.Interfaces;
public interface ISoftDeletable
{
    bool IsDeleted {get;}
    DateTimeOffset? DeletedAt {get;}
    string? DeletedBy {get;}
}