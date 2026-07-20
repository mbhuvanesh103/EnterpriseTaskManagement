namespace EnterpriseTaskManagement.Domain.Common.Interfaces;
public interface IAuditable
{
    DateTimeOffset CreateAt {get;}
    String CreateBy{get;}
    DateTimeOffset? UpdatedAt {get;}
    String? UpdatedBy {get;}


}