using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices.Events;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Domain.Invoices;

/// <summary>
/// Invoice aggregate root. Created by the Subscription aggregate (either at
/// activation or billing cycle). Can be paid exactly once.
/// </summary>
public sealed class Invoice : AggregateRoot<InvoiceId>
{
    public CustomerId CustomerId { get; private set; } = default!;
    public SubscriptionId SubscriptionId { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public DateTime PeriodStartUtc { get; private set; }
    public DateTime PeriodEndUtc { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

#pragma warning disable CS8618
    private Invoice() { }
#pragma warning restore CS8618

    private Invoice(
        InvoiceId id,
        CustomerId customerId,
        SubscriptionId subscriptionId,
        Money amount,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        DateTime issuedAtUtc)
        : base(id)
    {
        CustomerId = customerId;
        SubscriptionId = subscriptionId;
        Amount = amount;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        Status = InvoiceStatus.Issued;
        IssuedAtUtc = issuedAtUtc;
    }

    internal static Invoice Issue(
        CustomerId customerId,
        SubscriptionId subscriptionId,
        Money amount,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        DateTime nowUtc)
    {
        if (periodEndUtc <= periodStartUtc)
        {
            throw new DomainException("Invoice period end must be after period start.");
        }

        if (amount.IsZero)
        {
            throw new DomainException("Invoice amount must be greater than zero.");
        }

        var invoice = new Invoice(
            InvoiceId.New(),
            customerId,
            subscriptionId,
            amount,
            periodStartUtc,
            periodEndUtc,
            nowUtc);

        invoice.Raise(new InvoiceGenerated(
            invoice.Id,
            subscriptionId,
            customerId,
            amount,
            periodStartUtc,
            periodEndUtc));

        return invoice;
    }

    /// <summary>
    /// Marks the invoice as Paid. Invariant: an invoice can only be paid once.
    /// A Void invoice cannot be paid.
    /// </summary>
    public void Pay(DateTime nowUtc)
    {
        switch (Status)
        {
            case InvoiceStatus.Paid:
                throw new DomainException($"Invoice {Id} has already been paid.");
            case InvoiceStatus.Void:
                throw new DomainException($"Invoice {Id} is void and cannot be paid.");
        }

        Status = InvoiceStatus.Paid;
        PaidAtUtc = nowUtc;

        Raise(new PaymentReceived(Id, CustomerId, Amount, nowUtc));
    }
}
