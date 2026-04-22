using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Domain.Invoices;

public sealed class InvoiceId : ValueObject
{
    public Guid Value { get; }

    private InvoiceId(Guid value) => Value = value;

    public static InvoiceId New() => new(Guid.NewGuid());
    public static InvoiceId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException("InvoiceId cannot be empty.");
        }
        return new InvoiceId(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
