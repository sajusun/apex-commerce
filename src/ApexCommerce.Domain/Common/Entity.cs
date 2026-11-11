namespace ApexCommerce.Domain.Common;

/// <summary>
/// Base class for all domain entities having identity.
/// In Laravel: Like an abstract base model, but without ActiveRecord DB bindings.
/// </summary>
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; protected set; }

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (EqualityComparer<TId>.Default.Equals(Id, default)) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => (GetType().ToString() + Id).GetHashCode();
}

