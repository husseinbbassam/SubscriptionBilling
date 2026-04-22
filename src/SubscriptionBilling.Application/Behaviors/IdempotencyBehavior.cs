using System.Text.Json;
using MediatR;
using SubscriptionBilling.Application.Abstractions;

namespace SubscriptionBilling.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior: if a command implements IIdempotentCommand and
/// the supplied key has already been processed, return the previous response
/// instead of re-executing the handler.
/// </summary>
public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IIdempotencyStore _store;

    public IdempotencyBehavior(IIdempotencyStore store) => _store = store;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IIdempotentCommand<TResponse> idempotent)
        {
            return await next();
        }

        var existing = await _store.GetAsync(idempotent.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var cached = JsonSerializer.Deserialize<TResponse>(existing.ResponseJson);
            if (cached is not null)
            {
                return cached;
            }
        }

        var response = await next();

        await _store.SaveAsync(
            idempotent.IdempotencyKey,
            typeof(TRequest).FullName ?? typeof(TRequest).Name,
            JsonSerializer.Serialize(response),
            cancellationToken);

        return response;
    }
}
