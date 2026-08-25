using EnterpriseTaskManagement.Domain.Common;

namespace EnterpriseTaskManagement.Domain.Events.Users;

/// <summary>Raised when a new user is registered in the organization.</summary>
public sealed record UserRegisteredDomainEvent(
    Guid UserPublicId,
    string Email,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
