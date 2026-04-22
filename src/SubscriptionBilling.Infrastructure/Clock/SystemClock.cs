using SubscriptionBilling.Domain.Abstractions;

namespace SubscriptionBilling.Infrastructure.Clock;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
