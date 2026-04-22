namespace SubscriptionBilling.Infrastructure.Outbox;

/// <summary>
/// Persisted record of a domain event that was raised as part of a committed
/// transaction. A background dispatcher (not included in this sample — see
/// README) reads unprocessed rows and publishes them.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public string? Error { get; set; }
}
