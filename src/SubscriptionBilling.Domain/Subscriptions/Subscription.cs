using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.Subscriptions.Events;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Domain.Subscriptions;

/// <summary>
/// Subscription aggregate root. Drives the lifecycle:
///   Created (PendingActivation) -> Activated -> (billing cycles) -> Canceled
///
/// Invariants enforced here:
///   * Only Active subscriptions can issue invoices on a billing cycle.
///   * Activation flips status exactly once.
///   * Cancellation is terminal — cancel then cancel throws.
///   * Activation generates the first invoice for the opening period.
///
/// NOTE on aggregate boundaries: Subscription does NOT hold a collection of
/// Invoice entities. Invoices are their own aggregate (so paying one doesn't
/// require loading the whole subscription history). The Subscription returns
/// the newly-created Invoice for the caller to hand to the IInvoiceRepository.
/// </summary>
public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    public CustomerId CustomerId { get; private set; } = default!;
    public string PlanName { get; private set; } = default!;
    public Money Price { get; private set; } = default!;
    public BillingCycle BillingCycle { get; private set; } = default!;
    public SubscriptionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ActivatedAtUtc { get; private set; }
    public DateTime? CanceledAtUtc { get; private set; }

    /// <summary>
    /// Start of the current open billing period. After each invoice is generated,
    /// this advances by one cycle. Null until activated.
    /// </summary>
    public DateTime? CurrentPeriodStartUtc { get; private set; }

#pragma warning disable CS8618
    private Subscription() { }
#pragma warning restore CS8618

    private Subscription(
        SubscriptionId id,
        CustomerId customerId,
        string planName,
        Money price,
        BillingCycle billingCycle,
        DateTime nowUtc)
        : base(id)
    {
        CustomerId = customerId;
        PlanName = planName;
        Price = price;
        BillingCycle = billingCycle;
        Status = SubscriptionStatus.PendingActivation;
        CreatedAtUtc = nowUtc;
    }

    public static Subscription Create(
        CustomerId customerId,
        string planName,
        Money price,
        BillingCycle billingCycle,
        DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(planName))
        {
            throw new DomainException("Plan name is required.");
        }

        if (price.IsZero)
        {
            throw new DomainException("Subscription price must be greater than zero.");
        }

        return new Subscription(SubscriptionId.New(), customerId, planName.Trim(), price, billingCycle, nowUtc);
    }

    /// <summary>
    /// Activates the subscription and issues the first invoice for the opening
    /// billing period. Returns the new Invoice so the application layer can
    /// persist it through the IInvoiceRepository.
    /// </summary>
    public Invoice Activate(DateTime nowUtc)
    {
        if (Status == SubscriptionStatus.Active)
        {
            throw new DomainException($"Subscription {Id} is already active.");
        }

        if (Status == SubscriptionStatus.Canceled)
        {
            throw new DomainException($"Subscription {Id} has been canceled and cannot be activated.");
        }

        Status = SubscriptionStatus.Active;
        ActivatedAtUtc = nowUtc;
        CurrentPeriodStartUtc = nowUtc;

        var periodEnd = BillingCycle.Advance(nowUtc);
        var invoice = Invoice.Issue(CustomerId, Id, Price, nowUtc, periodEnd, nowUtc);

        Raise(new SubscriptionActivated(Id, CustomerId, nowUtc));

        // Advance the open period — the next invoice will be for the period
        // starting at periodEnd.
        CurrentPeriodStartUtc = periodEnd;

        return invoice;
    }

    /// <summary>
    /// Generates the next billing-cycle invoice. Called by the billing background
    /// job for every Active subscription whose current period start is in the past.
    /// Returns null if the subscription is not in a state where it should issue one.
    /// </summary>
    public Invoice? GenerateNextInvoice(DateTime nowUtc)
    {
        if (Status != SubscriptionStatus.Active)
        {
            return null;
        }

        if (CurrentPeriodStartUtc is null)
        {
            throw new DomainException(
                $"Subscription {Id} is Active but has no current period — invariant broken.");
        }

        if (CurrentPeriodStartUtc > nowUtc)
        {
            // Not yet due.
            return null;
        }

        var periodStart = CurrentPeriodStartUtc.Value;
        var periodEnd = BillingCycle.Advance(periodStart);
        var invoice = Invoice.Issue(CustomerId, Id, Price, periodStart, periodEnd, nowUtc);

        CurrentPeriodStartUtc = periodEnd;
        return invoice;
    }

    /// <summary>
    /// Cancels the subscription. Existing invoices (history) remain untouched;
    /// no new invoices will be generated.
    /// </summary>
    public void Cancel(DateTime nowUtc)
    {
        if (Status == SubscriptionStatus.Canceled)
        {
            throw new DomainException($"Subscription {Id} is already canceled.");
        }

        Status = SubscriptionStatus.Canceled;
        CanceledAtUtc = nowUtc;
        CurrentPeriodStartUtc = null;
    }
}
