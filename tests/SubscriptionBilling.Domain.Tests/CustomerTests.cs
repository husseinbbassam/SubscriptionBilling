using FluentAssertions;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.ValueObjects;
using Xunit;

namespace SubscriptionBilling.Domain.Tests;

public class CustomerTests
{
    [Fact]
    public void Create_WithBlankName_Throws()
    {
        var act = () => Customer.Create("   ", Email.Of("a@b.com"));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_AssignsFields()
    {
        var customer = Customer.Create("Acme", Email.Of("ops@acme.com"));
        customer.Name.Should().Be("Acme");
        customer.Email.Value.Should().Be("ops@acme.com");
        customer.Id.Should().NotBeNull();
    }

    [Fact]
    public void Rename_WithBlank_Throws()
    {
        var customer = Customer.Create("Acme", Email.Of("a@b.com"));
        var act = () => customer.Rename(" ");
        act.Should().Throw<DomainException>();
    }
}
