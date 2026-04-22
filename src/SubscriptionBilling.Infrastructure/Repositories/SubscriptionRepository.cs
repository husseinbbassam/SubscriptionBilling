using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class SubscriptionRepository : ISubscriptionRepository
{
    private readonly BillingDbContext _db;

    public SubscriptionRepository(BillingDbContext db) => _db = db;

    public async Task AddAsync(Subscription subscription, CancellationToken ct = default)
    {
        await _db.Subscriptions.AddAsync(subscription, ct);
    }

    public Task<Subscription?> GetAsync(SubscriptionId id, CancellationToken ct = default) =>
        _db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Subscription>> GetDueForBillingAsync(
        DateTime cutoffUtc,
        CancellationToken ct = default)
    {
        var list = await _db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active
                     && s.CurrentPeriodStartUtc != null
                     && s.CurrentPeriodStartUtc <= cutoffUtc)
            .ToListAsync(ct);
        return list;
    }
}
