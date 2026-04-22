namespace SubscriptionBilling.Application.Abstractions;

/// <summary>
/// Records the result of handled commands keyed by an idempotency key so the
/// same command can be retried without side-effects.
/// </summary>
public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string key, CancellationToken ct = default);
    Task SaveAsync(string key, string commandType, string responseJson, CancellationToken ct = default);
}

public sealed record IdempotencyRecord(string Key, string CommandType, string ResponseJson, DateTime CreatedAtUtc);
