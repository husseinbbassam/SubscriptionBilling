using MediatR;

namespace SubscriptionBilling.Application.Abstractions;

/// <summary>
/// Marker for commands that should be deduplicated via the idempotency store.
/// The client supplies the IdempotencyKey; duplicate keys return the cached
/// response without re-executing the handler.
/// </summary>
public interface IIdempotentCommand<TResponse> : IRequest<TResponse>
{
    string IdempotencyKey { get; }
}
