namespace SubscriptionBilling.Domain.SeedWork;

/// <summary>
/// Marker interface for domain events. Domain events are raised by aggregates
/// and represent something meaningful that has happened in the domain.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOnUtc { get; }
}
