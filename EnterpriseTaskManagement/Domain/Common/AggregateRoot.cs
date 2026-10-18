namespace EnterpriseTaskManagement.Domain.Common;

/// <summary>
/// Consistency boundary of the domain model. Only aggregate roots are loaded
/// and saved by repositories, and they are the only place invariants spanning
/// several objects may be enforced. Raised domain events are collected here and
/// drained by the infrastructure after the unit of work commits.
/// </summary>
public abstract class AggregateRoot<TId> : AuditableEntity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id)
        : base(id)
    {
    }

    /// <summary>Events raised by this aggregate and not yet dispatched.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
