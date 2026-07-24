using EnterpriseTaskManagement.Domain.Common;

namespace EnterpriseTaskManagement.Domain.Events.User;

public sealed class UserCreatedEvent : IDomainEvent
{
    public string UserPublicId { get; }

    public UserCreatedEvent(string userPublicId)
    {
        UserPublicId = userPublicId;
    }
}