using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Idempotency;

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly BillingDbContext _db;

    public IdempotencyStore(BillingDbContext db) => _db = db;

    public async Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default)
    {
        var entry = await _db.IdempotencyEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Key == key, ct);
        return entry is null
            ? null
            : new IdempotencyRecord(entry.Key, entry.CommandType, entry.ResponseJson, entry.CreatedAtUtc);
    }

    public async Task SaveAsync(string key, string commandType, string responseJson, CancellationToken ct = default)
    {
        _db.IdempotencyEntries.Add(new IdempotencyEntry
        {
            Key = key,
            CommandType = commandType,
            ResponseJson = responseJson,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
