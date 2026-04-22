using SubscriptionBilling.Domain.Abstractions;

namespace SubscriptionBilling.Domain.Tests.Support;

public sealed class TestClock : IClock
{
    public DateTime UtcNow { get; private set; }

    public TestClock(DateTime start) => UtcNow = start;

    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
    public void Set(DateTime moment) => UtcNow = moment;
}
