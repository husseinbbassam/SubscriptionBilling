using SubscriptionBilling.Domain.SeedWork;

namespace SubscriptionBilling.Domain.ValueObjects;

/// <summary>
/// The length of a billing period. Implemented as a value object rather than
/// a raw enum so we can keep the "advance a date" logic co-located with the type.
/// </summary>
public sealed class BillingCycle : ValueObject
{
    public static readonly BillingCycle Monthly = new(nameof(Monthly));
    public static readonly BillingCycle Yearly = new(nameof(Yearly));

    public string Name { get; }

    private BillingCycle(string name) => Name = name;

    public static BillingCycle FromName(string name) => name switch
    {
        nameof(Monthly) => Monthly,
        nameof(Yearly) => Yearly,
        _ => throw new DomainException($"Unknown billing cycle '{name}'.")
    };

    /// <summary>
    /// Returns the start of the next billing period given the current one's start.
    /// </summary>
    public DateTime Advance(DateTime fromUtc) => this == Monthly
        ? fromUtc.AddMonths(1)
        : fromUtc.AddYears(1);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
    }

    public override string ToString() => Name;
}
