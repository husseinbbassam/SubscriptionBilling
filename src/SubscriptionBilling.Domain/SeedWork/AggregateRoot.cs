namespace SubscriptionBilling.Domain.SeedWork;

/// <summary>
/// Base class for aggregate roots. Owns a buffer of domain events that the
/// infrastructure layer drains after a successful SaveChanges and hands off
/// to the outbox.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id) { }

#pragma warning disable CS8618
    protected AggregateRoot() { }
#pragma warning restore CS8618

    protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
