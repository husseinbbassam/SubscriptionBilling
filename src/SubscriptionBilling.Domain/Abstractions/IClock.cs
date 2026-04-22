namespace SubscriptionBilling.Domain.Abstractions;

/// <summary>
/// Abstraction over "now" so the domain and tests can control time.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
