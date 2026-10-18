using EnterpriseTaskManagement.Domain.Common;

namespace EnterpriseTaskManagement.Domain.Events.Users;

/// <summary>Raised when a user's email address changes.</summary>
public sealed record UserEmailChangedDomainEvent(
    Guid UserPublicId,
    string PreviousEmail,
    string NewEmail,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
