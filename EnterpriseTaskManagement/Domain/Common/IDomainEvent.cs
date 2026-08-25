namespace EnterpriseTaskManagement.Domain.Common;

/// <summary>
/// Marker for something meaningful that happened inside the domain.
/// Kept framework-free: the Application layer adapts these to MediatR
/// notifications, the Domain never references a dispatcher.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Moment the event occurred (UTC).</summary>
    DateTimeOffset OccurredOnUtc { get; }
}
