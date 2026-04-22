using FluentAssertions;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.ValueObjects;
using Xunit;

namespace SubscriptionBilling.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Of_WithNegativeAmount_Throws()
    {
        var act = () => Money.Of(-1m, "USD");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Of_WithInvalidCurrency_Throws()
    {
        var act = () => Money.Of(10m, "US");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Of_NormalizesCurrencyCase()
    {
        Money.Of(10m, "usd").Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        var a = Money.Of(10m, "USD");
        var b = Money.Of(10m, "EUR");
        var act = () => a.Add(b);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Equality_IsStructural()
    {
        Money.Of(10m, "USD").Should().Be(Money.Of(10m, "USD"));
    }
}
