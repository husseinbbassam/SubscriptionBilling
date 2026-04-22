using FluentAssertions;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Invoices.Events;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.Tests.Support;
using SubscriptionBilling.Domain.ValueObjects;
using Xunit;

namespace SubscriptionBilling.Domain.Tests;

public class InvoiceTests
{
    private static readonly DateTime Start = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static Invoice NewInvoice(TestClock clock)
    {
        var subscription = Subscription.Create(
            CustomerId.New(),
            "Pro",
            Money.Of(9.99m, "USD"),
            BillingCycle.Monthly,
            clock.UtcNow);
        return subscription.Activate(clock.UtcNow);
    }

    [Fact]
    public void Pay_MarksInvoiceAsPaid()
    {
        var clock = new TestClock(Start);
        var invoice = NewInvoice(clock);

        clock.Advance(TimeSpan.FromDays(1));
        invoice.Pay(clock.UtcNow);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAtUtc.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Pay_RaisesPaymentReceivedEvent()
    {
        var clock = new TestClock(Start);
        var invoice = NewInvoice(clock);
        invoice.ClearDomainEvents(); // drop the InvoiceGenerated from Activate

        invoice.Pay(clock.UtcNow);

        invoice.DomainEvents.Should().ContainSingle(e => e is PaymentReceived);
    }

    [Fact]
    public void Pay_Twice_Throws()
    {
        var clock = new TestClock(Start);
        var invoice = NewInvoice(clock);

        invoice.Pay(clock.UtcNow);
        var act = () => invoice.Pay(clock.UtcNow);

        act.Should().Throw<DomainException>()
            .WithMessage("*already been paid*");
    }
}
