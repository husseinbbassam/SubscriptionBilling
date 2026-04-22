using FluentAssertions;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.ValueObjects;
using Xunit;

namespace SubscriptionBilling.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    public void Of_WithInvalidInput_Throws(string input)
    {
        var act = () => Email.Of(input);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Of_LowercasesAndTrims()
    {
        Email.Of("  Foo@Example.COM ").Value.Should().Be("foo@example.com");
    }
}
