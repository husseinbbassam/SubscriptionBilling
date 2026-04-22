using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Domain.Subscriptions;

public sealed class SubscriptionId : ValueObject
{
    public Guid Value { get; }

    private SubscriptionId(Guid value) => Value = value;

    public static SubscriptionId New() => new(Guid.NewGuid());
    public static SubscriptionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("SubscriptionId cannot be empty.");
        }
        return new SubscriptionId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
