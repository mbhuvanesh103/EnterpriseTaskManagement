namespace EnterpriseTaskManagement.Domain.Common;

/// <summary>
/// Base class for all domain entities. Identity-based equality: two entities
/// of the same type are equal when they share the same non-default identifier.
/// </summary>
/// <typeparam name="TId">Type of the entity identifier.</typeparam>
public abstract class BaseEntity<TId> : IEquatable<BaseEntity<TId>>
    where TId : notnull
{
    protected BaseEntity(TId id) => Id = id;

    /// <summary>Persistence identity of the entity.</summary>
    public TId Id { get; protected set; }

    /// <summary>True when the entity has not been assigned an identity yet.</summary>
    public bool IsTransient => EqualityComparer<TId>.Default.Equals(Id, default!);

    public bool Equals(BaseEntity<TId>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (IsTransient || other.IsTransient)
        {
            return false;
        }

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as BaseEntity<TId>);

    public override int GetHashCode() =>
        IsTransient ? base.GetHashCode() : HashCode.Combine(GetType(), Id);

    public static bool operator ==(BaseEntity<TId>? left, BaseEntity<TId>? right) =>
        Equals(left, right);

    public static bool operator !=(BaseEntity<TId>? left, BaseEntity<TId>? right) =>
        !Equals(left, right);
}

/// <summary>Convenience base entity using <see cref="long"/> identities.</summary>
public abstract class BaseEntity : BaseEntity<long>
{
    protected BaseEntity(long id = default)
        : base(id)
    {
    }
}
