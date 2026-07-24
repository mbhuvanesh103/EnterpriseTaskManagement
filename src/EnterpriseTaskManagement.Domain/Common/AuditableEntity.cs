using EnterpriseTaskManagement.Domain.Common.Interfaces;

namespace EnterpriseTaskManagement.Domain.Common;

public abstract class AuditableEntity : AggregateRoot,
    IAuditable,
    IHasPublicId,
    ISoftDeletable,
    IHasConcurrencyToken
{
    protected AuditableEntity()
    {
        PublicId = Guid.NewGuid().ToString("N");
    }

    public string PublicId { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public string CreatedBy { get; protected set; } = string.Empty;

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public string? UpdatedBy { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public DateTimeOffset? DeletedAt { get; protected set; }

    public string? DeletedBy { get; protected set; }

    public byte[] Version { get; protected set; } = Array.Empty<byte>();
}