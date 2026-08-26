using EnterpriseTaskManagement.Domain.Common;

namespace EnterpriseTaskManagement.Domain.Events.Users;

/// <summary>Raised when a user is soft-deleted (deactivated and archived).</summary>
public sealed record UserDeletedDomainEvent(
    Guid UserPublicId,
    string Email,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
