using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Domain.Invoices.Events;

public sealed record PaymentReceived(
    InvoiceId InvoiceId,
    CustomerId CustomerId,
    Money Amount,
    DateTime PaidAtUtc
) : DomainEvent;
