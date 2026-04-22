namespace SubscriptionBilling.Domain.SeedWork;

/// <summary>
/// Convenience base for immutable record-style domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
