using FluentAssertions;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Invoices.Events;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.Subscriptions.Events;
using SubscriptionBilling.Domain.Tests.Support;
using SubscriptionBilling.Domain.ValueObjects;
using Xunit;

namespace SubscriptionBilling.Domain.Tests;

public class SubscriptionTests
{
    private static readonly DateTime Start = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static Subscription NewSubscription(TestClock clock)
    {
        return Subscription.Create(
            CustomerId.New(),
            "Pro",
            Money.Of(9.99m, "USD"),
            BillingCycle.Monthly,
            clock.UtcNow);
    }

    [Fact]
    public void Activate_GeneratesFirstInvoice()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);

        var invoice = subscription.Activate(clock.UtcNow);

        invoice.Should().NotBeNull();
        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.Amount.Should().Be(Money.Of(9.99m, "USD"));
        invoice.PeriodStartUtc.Should().Be(Start);
        invoice.PeriodEndUtc.Should().Be(Start.AddMonths(1));
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Activate_RaisesSubscriptionActivatedEvent()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);

        subscription.Activate(clock.UtcNow);

        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionActivated);
    }

    [Fact]
    public void Activate_FirstInvoice_RaisesInvoiceGeneratedEvent()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);

        var invoice = subscription.Activate(clock.UtcNow);

        invoice.DomainEvents.Should().ContainSingle(e => e is InvoiceGenerated);
    }

    [Fact]
    public void Activate_Twice_Throws()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);
        subscription.Activate(clock.UtcNow);

        var act = () => subscription.Activate(clock.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_PreventsFutureInvoices()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);
        subscription.Activate(clock.UtcNow);

        // Move past the current period so the subscription would normally be due.
        clock.Advance(TimeSpan.FromDays(40));
        subscription.Cancel(clock.UtcNow);

        var invoice = subscription.GenerateNextInvoice(clock.UtcNow);

        invoice.Should().BeNull();
        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
    }

    [Fact]
    public void Cancel_DoesNotVoidExistingInvoices()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);
        var firstInvoice = subscription.Activate(clock.UtcNow);

        subscription.Cancel(clock.UtcNow);

        firstInvoice.Status.Should().Be(InvoiceStatus.Issued);
    }

    [Fact]
    public void Cancel_Twice_Throws()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);
        subscription.Activate(clock.UtcNow);
        subscription.Cancel(clock.UtcNow);

        var act = () => subscription.Cancel(clock.UtcNow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GenerateNextInvoice_WhenDue_IssuesInvoiceForNextPeriod()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);
        subscription.Activate(clock.UtcNow);

        clock.Advance(TimeSpan.FromDays(31));
        var next = subscription.GenerateNextInvoice(clock.UtcNow);

        next.Should().NotBeNull();
        next!.PeriodStartUtc.Should().Be(Start.AddMonths(1));
        next.PeriodEndUtc.Should().Be(Start.AddMonths(2));
    }

    [Fact]
    public void GenerateNextInvoice_BeforePeriodRolls_ReturnsNull()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);
        subscription.Activate(clock.UtcNow);

        // Only a few days in — not yet due.
        clock.Advance(TimeSpan.FromDays(5));
        var next = subscription.GenerateNextInvoice(clock.UtcNow);

        next.Should().BeNull();
    }

    [Fact]
    public void GenerateNextInvoice_OnPendingSubscription_ReturnsNull()
    {
        var clock = new TestClock(Start);
        var subscription = NewSubscription(clock);

        var next = subscription.GenerateNextInvoice(clock.UtcNow);

        next.Should().BeNull();
    }

    [Fact]
    public void Create_WithZeroPrice_Throws()
    {
        var clock = new TestClock(Start);
        var act = () => Subscription.Create(
            CustomerId.New(),
            "Pro",
            Money.Of(0m, "USD"),
            BillingCycle.Monthly,
            clock.UtcNow);
        act.Should().Throw<DomainException>();
    }
}
