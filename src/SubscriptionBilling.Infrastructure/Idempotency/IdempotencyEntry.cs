namespace SubscriptionBilling.Infrastructure.Idempotency;

public sealed class IdempotencyEntry
{
    public string Key { get; set; } = default!;
    public string CommandType { get; set; } = default!;
    public string ResponseJson { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
}
