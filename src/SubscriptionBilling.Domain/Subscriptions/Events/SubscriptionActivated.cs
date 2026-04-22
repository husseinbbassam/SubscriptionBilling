using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Domain.Subscriptions.Events;

public sealed record SubscriptionActivated(
    SubscriptionId SubscriptionId,
    CustomerId CustomerId,
    DateTime ActivatedAtUtc
) : DomainEvent;
