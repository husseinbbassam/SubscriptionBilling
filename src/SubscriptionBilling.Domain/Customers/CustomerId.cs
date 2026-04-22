using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Domain.Customers;

public sealed class CustomerId : ValueObject
{
    public Guid Value { get; }

    private CustomerId(Guid value) => Value = value;

    public static CustomerId New() => new(Guid.NewGuid());
    public static CustomerId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("CustomerId cannot be empty.");
        }
        return new CustomerId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
