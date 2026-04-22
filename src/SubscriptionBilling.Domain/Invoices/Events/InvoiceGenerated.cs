using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Domain.Invoices.Events;

public sealed record InvoiceGenerated(
    InvoiceId InvoiceId,
    SubscriptionId SubscriptionId,
    CustomerId CustomerId,
    Money Amount,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc
) : DomainEvent;
