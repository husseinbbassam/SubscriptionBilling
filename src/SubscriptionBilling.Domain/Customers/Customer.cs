using SubscriptionBilling.Domain.SeedWork;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Domain.Customers;

/// <summary>
/// Customer aggregate root. Owns identity + contact info. Does NOT hold
/// subscriptions as a collection — Subscription is its own aggregate to keep
/// consistency boundaries small.
/// </summary>
public sealed class Customer : AggregateRoot<CustomerId>
{
    public string Name { get; private set; } = default!;
    public Email Email { get; private set; } = default!;

#pragma warning disable CS8618 // EF materialization
    private Customer() { }
#pragma warning restore CS8618

    private Customer(CustomerId id, string name, Email email) : base(id)
    {
        Name = name;
        Email = email;
    }

    public static Customer Create(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Customer name is required.");
        }

        return new Customer(CustomerId.New(), name.Trim(), email);
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new DomainException("Customer name is required.");
        }

        Name = newName.Trim();
    }
}
