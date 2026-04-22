namespace SubscriptionBilling.Domain.SeedWork;

/// <summary>
/// Base class for domain entities. Equality is based on the identity (Id), not structural equality.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; }

    protected Entity(TId id)
    {
        if (id is null)
        {
            throw new ArgumentNullException(nameof(id));
        }
        Id = id;
    }

#pragma warning disable CS8618 // EF Core materialization constructor
    protected Entity() { }
#pragma warning restore CS8618

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj) => obj is Entity<TId> e && Equals(e);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
