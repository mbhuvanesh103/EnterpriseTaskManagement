namespace EnterpriseTaskManagement.Domain.Common.Interfaces;
public interface IHasConcurrencyToken
{
    byte[] Version {get;}
}