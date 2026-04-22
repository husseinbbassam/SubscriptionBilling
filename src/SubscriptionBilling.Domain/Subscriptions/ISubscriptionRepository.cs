namespace SubscriptionBilling.Domain.Subscriptions;

public interface ISubscriptionRepository
{
    Task AddAsync(Subscription subscription, CancellationToken ct = default);
    Task<Subscription?> GetAsync(SubscriptionId id, CancellationToken ct = default);

    /// <summary>
    /// Returns all Active subscriptions whose current period start is at or
    /// before the supplied cutoff. Used by the billing background job.
    /// </summary>
    Task<IReadOnlyList<Subscription>> GetDueForBillingAsync(DateTime cutoffUtc, CancellationToken ct = default);
}
