namespace EnterpriseTaskManagement.Domain.Common;

/// <summary>
/// Entity that tracks who created/changed it and when, plus soft-delete state.
/// Values are set through domain methods only — never by persistence
/// annotations — so the Domain layer stays framework independent.
/// </summary>
public abstract class AuditableEntity<TId> : BaseEntity<TId>
    where TId : notnull
{
    protected AuditableEntity(TId id)
        : base(id)
    {
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    /// <summary>Stamps creation audit values. Called once, on first persist.</summary>
    public void MarkCreated(DateTimeOffset whenUtc, string? by = null)
    {
        CreatedAtUtc = whenUtc;
        CreatedBy = by;
    }

    /// <summary>Stamps modification audit values.</summary>
    public void MarkModified(DateTimeOffset whenUtc, string? by = null)
    {
        ModifiedAtUtc = whenUtc;
        ModifiedBy = by;
    }

    /// <summary>Marks the entity as soft-deleted.</summary>
    public void MarkDeleted(DateTimeOffset whenUtc, string? by = null)
    {
        IsDeleted = true;
        DeletedAtUtc = whenUtc;
        DeletedBy = by;
    }

    /// <summary>Reverts a soft delete.</summary>
    public void MarkRestored()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
